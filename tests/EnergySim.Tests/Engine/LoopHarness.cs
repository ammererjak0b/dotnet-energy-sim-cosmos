using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

/// <summary>Runs the real loop on a FakeTimeProvider. One tick = Advance(tick) + wait for the next published frame.</summary>
public sealed class LoopHarness
{
    private const int STOP_TIMEOUT_SECONDS = 5;

    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

    private Task? _runTask;

    public LoopHarness(bool needsWeather)
    {
        Device = new FakeDevice(needsWeather);
        Options.LocationId = "salzburg";
    }

    public FakeTimeProvider TimeProvider { get; } = new FakeTimeProvider();

    public FakeClockSource Clock { get; } = new FakeClockSource();

    public FakeWeatherClient Weather { get; } = new FakeWeatherClient();

    public FakeSimulationSink Sink { get; } = new FakeSimulationSink();

    public FakeDevice Device { get; }

    public Channel<DeviceCommand> Commands { get; } = Channel.CreateUnbounded<DeviceCommand>();

    public SimulationOptions Options { get; } = new SimulationOptions();

    public bool UseWeatherClient { get; set; } = true; // false => loop gets null

    public Task RunTask
    {
        get
        {
            if (_runTask == null)
            {
                throw new InvalidOperationException("not started");
            }

            return _runTask;
        }
    }

    public TimeSpan Tick
    {
        get
        {
            return TimeSpan.FromMilliseconds(Options.TickMilliseconds);
        }
    }

    /// <summary>Starts the loop; the first frame is published immediately.</summary>
    public async Task startAsync()
    {
        FakeWeatherClient? weatherClient = null;
        if (UseWeatherClient)
        {
            weatherClient = Weather;
        }

        DeviceSimulationLoop loop = new DeviceSimulationLoop(
            Device,
            Clock,
            weatherClient,
            Sink,
            Commands.Reader,
            Options,
            TimeProvider,
            NullLogger<DeviceSimulationLoop>.Instance);
        _runTask = loop.runAsync(_cancellation.Token);
        await waitForFramesAsync(1);
    }

    /// <summary>Real time passes one tick, the fake clock moves along (speed applied), loop publishes once.</summary>
    public async Task tickAsync()
    {
        Clock.advance(Tick);
        await advanceRealTimeAsync(Tick);
    }

    /// <summary>Only the loop timer moves; VirtualNow stays whatever the test set.</summary>
    public async Task advanceRealTimeAsync(TimeSpan realElapsed)
    {
        int frameCount = Sink.FrameCount;
        TimeProvider.Advance(realElapsed);
        await waitForFramesAsync(frameCount + 1);
    }

    public async Task stopAsync()
    {
        _cancellation.Cancel();
        await RunTask.WaitAsync(TimeSpan.FromSeconds(STOP_TIMEOUT_SECONDS));
    }

    private async Task waitForFramesAsync(int expectedCount)
    {
        await TestWait.untilAsync(delegate { return Sink.FrameCount >= expectedCount; });
        Assert.True(Sink.FrameCount >= expectedCount, "loop did not publish in time");
    }
}
