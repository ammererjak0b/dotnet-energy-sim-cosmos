using System.Threading.Channels;
using EnergySim.Cli;
using EnergySim.Terminal;
using EnergySim.Core.Devices;
using EnergySim.Pv;
using EnergySim.Core.Engine;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Cli;

public class ClockDashboardSourceTests
{
    private static readonly DateTimeOffset START = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider();
    private readonly LocalClock _clock;
    private readonly Channel<DeviceCommand> _channel = Channel.CreateUnbounded<DeviceCommand>();
    private readonly ClockDashboardSource _source;
    private readonly PvDevice _device = new PvDevice("pv-1", new PvOptions());
    private readonly LogBuffer _logBuffer = new LogBuffer(10);
    private int _shutdownCount;

    public ClockDashboardSourceTests()
    {
        _clock = new LocalClock(_timeProvider, START, 60.0);
        _source = new ClockDashboardSource(_device, "salzburg", _clock, _channel.Writer, _logBuffer, delegate { _shutdownCount++; });
    }

    private Task<CommandResult> executeAsync(string name, double value)
    {
        return _source.executeAsync(new DeviceCommand(name, value), CancellationToken.None);
    }

    private static CommandDescriptor find(DashboardState state, char key)
    {
        foreach (CommandDescriptor command in state.Commands)
        {
            if (command.Key == key)
            {
                return command;
            }
        }

        throw new InvalidOperationException("no command for key " + key);
    }

    [Fact]
    public void GetState_ListsDeviceCommandsThenClockKeys()
    {
        // Act
        DashboardState state = _source.getState();

        // Assert
        Assert.Equal('O', state.Commands[0].Key);
        Assert.Equal(ClockCommands.PAUSE, find(state, 'P').Name);
        Assert.Equal(ClockCommands.SET_SPEED, find(state, 'V').Name);
        Assert.True(find(state, 'V').HasParameter);
        Assert.Equal(ClockCommands.FASTER, find(state, '+').Name);
        Assert.Equal(ClockCommands.SLOWER, find(state, '-').Name);
    }

    [Fact]
    public void GetState_ShowsLiveClockAndDeviceIdentity()
    {
        // Act
        _timeProvider.Advance(TimeSpan.FromMinutes(1)); // 60 virtual minutes
        DashboardState state = _source.getState();

        // Assert
        Assert.Equal("pv-1", state.DeviceId);
        Assert.Equal("salzburg", state.LocationId);
        Assert.Equal(60.0, state.Speed);
        Assert.Equal("Running", state.ClockStatus);
        Assert.Contains("05:00Z", state.ClockText);
    }

    [Fact]
    public async Task PublishAsync_Frame_FillsHeaderRowsAndDeviceRows()
    {
        // Arrange
        SimulationFrame frame = new SimulationFrame();
        frame.Header.State = DeviceState.Running;
        frame.Header.ActivePowerW = 2500;
        frame.Header.EnergyWh = 12000;
        frame.Header.PowerLimitPercent = 80;
        frame.DisplayRows = new List<DisplayRow> { DeviceSupport.createRow("Irradiance", "800 W/m2") };

        // Act
        await _source.publishAsync(frame, CancellationToken.None);
        DashboardState state = _source.getState();

        // Assert
        Assert.Equal("2.50 kW", valueOf(state.HeaderRows, "Active power"));
        Assert.Equal("12.00 kWh", valueOf(state.HeaderRows, "Energy"));
        Assert.Equal("80 %", valueOf(state.HeaderRows, "Power limit"));
        Assert.Equal("Running", valueOf(state.HeaderRows, "State"));
        Assert.Equal("800 W/m2", valueOf(state.DeviceRows, "Irradiance"));
    }

    [Fact]
    public async Task PublishAsync_DeviceError_ShowsFaultBanner()
    {
        // Arrange
        SimulationFrame frame = new SimulationFrame();
        frame.DeviceError = "device error: boom";

        // Act
        await _source.publishAsync(frame, CancellationToken.None);

        // Assert
        DashboardState state = _source.getState();
        Assert.Contains("boom", state.BannerText);
        Assert.Equal(DisplaySeverity.Fault, state.BannerSeverity);
    }

    [Fact]
    public async Task ExecuteAsync_Pause_PausesThenResumes()
    {
        // Act
        CommandResult paused = await executeAsync(ClockCommands.PAUSE, 0);
        string statusWhilePaused = _source.getState().ClockStatus;
        string bannerWhilePaused = _source.getState().BannerText;
        CommandResult resumed = await executeAsync(ClockCommands.PAUSE, 0);

        // Assert
        Assert.True(paused.Accepted);
        Assert.Equal("Paused", statusWhilePaused);
        Assert.Contains("PAUSED", bannerWhilePaused);
        Assert.True(resumed.Accepted);
        Assert.Equal("Running", _source.getState().ClockStatus);
    }

    [Theory]
    [InlineData(ClockCommands.FASTER, 60.0, 120.0)]
    [InlineData(ClockCommands.SLOWER, 60.0, 30.0)]
    public async Task ExecuteAsync_FasterSlower_DoublesOrHalvesSpeed(string name, double speedBefore, double expectedSpeed)
    {
        // Arrange
        _clock.setSpeed(speedBefore);

        // Act
        await executeAsync(name, 0);

        // Assert
        Assert.Equal(expectedSpeed, _clock.getSnapshot().Speed);
    }

    [Fact]
    public async Task ExecuteAsync_SetSpeed_AppliesValue()
    {
        // Act
        CommandResult result = await executeAsync(ClockCommands.SET_SPEED, 500.0);

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(500.0, _clock.getSnapshot().Speed);
    }

    [Fact]
    public async Task ExecuteAsync_SetSpeedOutOfRange_IsRejected()
    {
        // Act
        CommandResult result = await executeAsync(ClockCommands.SET_SPEED, 99999.0);

        // Assert
        Assert.False(result.Accepted);
        Assert.Equal(60.0, _clock.getSnapshot().Speed);
    }

    [Fact]
    public async Task ExecuteAsync_DeviceCommand_GoesToDeviceChannel()
    {
        // Act
        await executeAsync(CommonCommands.SET_LIMIT, 40.0);

        // Assert
        DeviceCommand queued;
        Assert.True(_channel.Reader.TryRead(out queued!));
        Assert.Equal(CommonCommands.SET_LIMIT, queued.Name);
        Assert.Equal(40.0, queued.Value);
    }

    [Fact]
    public async Task WaitForFirstFrameAsync_CompletesAfterFirstPublish()
    {
        // Arrange
        Task waiting = _source.waitForFirstFrameAsync(CancellationToken.None);

        // Act
        bool completedBefore = waiting.IsCompleted;
        await _source.publishAsync(new SimulationFrame(), CancellationToken.None);
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.False(completedBefore);
        Assert.True(waiting.IsCompletedSuccessfully);
    }

    [Fact]
    public void GetState_ShowsNewestLogLines()
    {
        // Arrange
        _logBuffer.add("hello log");

        // Act
        DashboardState state = _source.getState();

        // Assert
        Assert.Contains("hello log", state.LogLines);
    }

    [Fact]
    public void RequestShutdown_CallsCallback()
    {
        // Act
        _source.requestShutdown();

        // Assert
        Assert.Equal(1, _shutdownCount);
    }

    [Theory]
    [InlineData(60.0, 2.0, 120.0)]
    [InlineData(2000.0, 2.0, 3600.0)] // clamped
    [InlineData(0.15, 0.5, 0.1)] // clamped
    public void NextSpeed_MultipliesAndClamps(double current, double factor, double expected)
    {
        // Act + Assert
        Assert.Equal(expected, ClockCommands.nextSpeed(current, factor));
    }

    private static string valueOf(List<DisplayRow> rows, string label)
    {
        foreach (DisplayRow row in rows)
        {
            if (row.Label == label)
            {
                return row.Value;
            }
        }

        throw new InvalidOperationException("no row " + label);
    }
}
