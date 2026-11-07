using EnergySim.Core.Engine;
using EnergySim.Modbus;
using EnergySim.Tests.Support;
using EnergySim.Time;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Time;

/// <summary>Participant registry of the master: real loopback sockets, fake time drives the heartbeats.</summary>
public sealed class ClockRegistryTests
{
    [Fact]
    public async Task GetParticipantViews_AfterRegistration_ShowsNewThenOkAfterFirstHeartbeat()
    {
        // Arrange
        await using RegistryHarness harness = new RegistryHarness();
        await harness.startAsync();
        int devicePort = harness.addDevice("pv-1");

        // Act
        await harness.registerAsync(devicePort);
        await harness.waitForParticipantCountAsync(1);
        ClockParticipantView registered = harness.Host.getParticipantViews()[0];
        await harness.waitUntilStatusAsync("pv-1", ClockParticipantStatus.Ok);
        ClockParticipantView afterHeartbeat = harness.Host.getParticipantViews()[0];

        // Assert
        Assert.Equal("pv-1", registered.DeviceId);
        Assert.Equal(devicePort, registered.Port);
        Assert.Equal(ClockParticipantStatus.New, registered.Status);
        Assert.Equal(ClockParticipantStatus.Ok, afterHeartbeat.Status);
        Assert.Equal(0, afterHeartbeat.MissedHeartbeats);
    }

    [Fact]
    public async Task GetParticipantViews_DeviceStopsAnswering_UnreachableAfterThreeMissedHeartbeatsAndStaysListed()
    {
        // Arrange
        await using RegistryHarness harness = new RegistryHarness();
        await harness.startAsync();
        int devicePort = harness.addDevice("battery-1");
        await harness.registerAsync(devicePort);
        await harness.waitUntilStatusAsync("battery-1", ClockParticipantStatus.Ok);
        await harness.stopDeviceAsync(devicePort);

        // Act
        await harness.missHeartbeatAsync("battery-1", 1);
        await harness.missHeartbeatAsync("battery-1", 2);
        ClockParticipantView afterTwo = harness.Host.getParticipantViews()[0];
        await harness.missHeartbeatAsync("battery-1", 3);
        List<ClockParticipantView> views = harness.Host.getParticipantViews();

        // Assert
        Assert.Equal(ClockParticipantStatus.Ok, afterTwo.Status);
        Assert.Single(views);
        Assert.Equal("battery-1", views[0].DeviceId);
        Assert.Equal(ClockParticipantStatus.Unreachable, views[0].Status);
        Assert.Equal(ClockServiceHost.MAX_MISSED_HEARTBEATS, views[0].MissedHeartbeats);
    }

    [Fact]
    public async Task GetParticipantViews_DuplicateDeviceId_RejectsSecondRegistration()
    {
        // Arrange
        await using RegistryHarness harness = new RegistryHarness();
        await harness.startAsync();
        int firstPort = harness.addDevice("charger-1");
        int secondPort = harness.addDevice("charger-1");
        await harness.registerAsync(firstPort);
        await harness.waitForParticipantCountAsync(1);

        // Act
        await harness.registerAsync(secondPort);
        await harness.waitForMessageAsync("Rejected duplicate device ID charger-1");
        List<ClockParticipantView> views = harness.Host.getParticipantViews();

        // Assert
        Assert.Single(views);
        Assert.Equal(firstPort, views[0].Port);
    }

    [Fact]
    public async Task GetParticipantViews_UnreachableHolderOfId_IsReplacedByNewRegistration()
    {
        // Arrange
        await using RegistryHarness harness = new RegistryHarness();
        await harness.startAsync();
        int firstPort = harness.addDevice("wind-1");
        await harness.registerAsync(firstPort);
        await harness.waitUntilStatusAsync("wind-1", ClockParticipantStatus.Ok);
        await harness.stopDeviceAsync(firstPort);
        await harness.missHeartbeatAsync("wind-1", 1);
        await harness.missHeartbeatAsync("wind-1", 2);
        await harness.missHeartbeatAsync("wind-1", 3);
        int secondPort = harness.addDevice("wind-1");

        // Act
        await harness.registerAsync(secondPort);
        await harness.waitForMessageAsync("Replaced unreachable wind-1");
        await harness.waitForParticipantCountAsync(1);
        List<ClockParticipantView> views = harness.Host.getParticipantViews();

        // Assert
        Assert.Single(views);
        Assert.Equal(secondPort, views[0].Port);
        Assert.NotEqual(ClockParticipantStatus.Unreachable, views[0].Status);
    }

    private sealed class RegistryHarness : IAsyncDisposable
    {
        private const int HEARTBEAT_MILLISECONDS = 1000;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly Dictionary<int, ClockEndpointServer> _devices = new Dictionary<int, ClockEndpointServer>();
        private readonly List<string> _messages = new List<string>();
        private readonly int _hostPort;
        private Task? _runTask;

        public RegistryHarness()
        {
            Time = new FakeTimeProvider();
            _hostPort = TestPorts.next(TestPorts.CLOCK_FIRST, TestPorts.CLOCK_LAST);
            TimeAuthority authority = new TimeAuthority(Time, DateTimeOffset.UnixEpoch, 60, TimeSpan.FromMilliseconds(100));
            TimeServiceOptions options = new TimeServiceOptions();
            options.Port = _hostPort;
            options.HeartbeatMilliseconds = HEARTBEAT_MILLISECONDS;
            Host = new ClockServiceHost(authority, Time, options);
            Host.StatusChanged += recordMessage;
        }

        public FakeTimeProvider Time { get; }

        public ClockServiceHost Host { get; }

        public Task startAsync()
        {
            _runTask = Task.Run(delegate { return Host.runAsync(_shutdown.Token); });
            return Task.CompletedTask;
        }

        public int addDevice(string deviceId)
        {
            ClockEndpointOptions options = new ClockEndpointOptions();
            options.Port = TestPorts.next(TestPorts.CLOCK_FIRST, TestPorts.CLOCK_LAST);
            options.DeviceId = deviceId;
            ClockEndpointServer device = new ClockEndpointServer(options);
            device.start();
            _devices[options.Port] = device;
            return options.Port;
        }

        public async Task stopDeviceAsync(int devicePort)
        {
            await _devices[devicePort].DisposeAsync();
            _devices.Remove(devicePort);
        }

        /// <summary>Writes the device port into the service, retries until the service listens.</summary>
        public async Task registerAsync(int devicePort)
        {
            bool registered = false;
            await TestWait.untilAsync(delegate
            {
                ClockModbusClient client = new ClockModbusClient("127.0.0.1", _hostPort, 1);
                try
                {
                    client.connect();
                    client.writeSingleRegister(0, devicePort);
                    registered = true;
                }
                catch (Exception)
                {
                    registered = false;
                }
                finally
                {
                    client.Dispose();
                }

                return registered;
            });
            Assert.True(registered, "could not register at the time service");
        }

        public async Task waitForParticipantCountAsync(int count)
        {
            await TestWait.untilAsync(delegate { return Host.getParticipantViews().Count == count; });
        }

        /// <summary>Advances fake time until a heartbeat round has delivered or missed for this device.</summary>
        public async Task waitUntilStatusAsync(string deviceId, ClockParticipantStatus status)
        {
            await TestWait.untilAsync(delegate
            {
                Time.Advance(TimeSpan.FromMilliseconds(HEARTBEAT_MILLISECONDS));
                return findStatus(deviceId) == status;
            });
        }

        /// <summary>One heartbeat round, then wait until the missed counter shows it.</summary>
        public async Task missHeartbeatAsync(string deviceId, int expectedMissed)
        {
            Time.Advance(TimeSpan.FromMilliseconds(HEARTBEAT_MILLISECONDS));
            await TestWait.untilAsync(delegate { return findMissed(deviceId) >= expectedMissed; });
        }

        public async Task waitForMessageAsync(string text)
        {
            await TestWait.untilAsync(delegate { return hasMessage(text); });
            Assert.True(hasMessage(text), "no status message containing: " + text);
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            if (_runTask != null)
            {
                try
                {
                    await _runTask;
                }
                catch (OperationCanceledException)
                {
                    // normal shutdown
                }
            }

            Host.StatusChanged -= recordMessage;
            Host.Dispose();
            foreach (ClockEndpointServer device in _devices.Values)
            {
                await device.DisposeAsync();
            }

            _shutdown.Dispose();
        }

        private void recordMessage(string message)
        {
            lock (_messages)
            {
                _messages.Add(message);
            }
        }

        private bool hasMessage(string text)
        {
            lock (_messages)
            {
                foreach (string message in _messages)
                {
                    if (message.Contains(text))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private ClockParticipantStatus findStatus(string deviceId)
        {
            foreach (ClockParticipantView view in Host.getParticipantViews())
            {
                if (view.DeviceId == deviceId)
                {
                    return view.Status;
                }
            }

            return ClockParticipantStatus.New;
        }

        private int findMissed(string deviceId)
        {
            foreach (ClockParticipantView view in Host.getParticipantViews())
            {
                if (view.DeviceId == deviceId)
                {
                    return view.MissedHeartbeats;
                }
            }

            return 0;
        }
    }
}
