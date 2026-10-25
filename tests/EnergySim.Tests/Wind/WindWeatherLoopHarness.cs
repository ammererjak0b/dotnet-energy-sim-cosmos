using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Tests.Engine;
using EnergySim.Wind;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Wind;

/// <summary>Connects the real wind device and loop to controlled weather, time and frame delivery.</summary>
public sealed class WindWeatherLoopHarness : ISimulationSink, IAsyncDisposable
{
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider();
    private readonly Channel<SimulationFrame> _frames = Channel.CreateUnbounded<SimulationFrame>();
    private readonly Channel<DeviceCommand> _commands = Channel.CreateUnbounded<DeviceCommand>();
    private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();
    private Task? _runTask;

    public WindDevice Device { get; } = new WindDevice("wind-weather", new WindOptions());

    public FakeClockSource Clock { get; } = new FakeClockSource();

    public FakeWeatherClient Weather { get; } = new FakeWeatherClient();

    public SimulationOptions Options { get; } = new SimulationOptions { LocationId = "salzburg" };

    /// <summary>Starts the loop and returns its initial frame without advancing virtual time.</summary>
    public async Task<SimulationFrame> startAsync()
    {
        if (_runTask != null)
        {
            throw new InvalidOperationException("loop already started");
        }

        DeviceSimulationLoop loop = new DeviceSimulationLoop(
            Device,
            Clock,
            Weather,
            this,
            _commands.Reader,
            Options,
            _timeProvider,
            NullLogger<DeviceSimulationLoop>.Instance);
        _runTask = loop.runAsync(_cancellation.Token);
        return await _frames.Reader.ReadAsync(_cancellation.Token);
    }

    /// <summary>Moves virtual time explicitly and drives one loop tick with fake time.</summary>
    public async Task<SimulationFrame> advanceAsync(TimeSpan virtualElapsed)
    {
        if (_runTask == null)
        {
            throw new InvalidOperationException("loop not started");
        }

        Clock.VirtualNow = Clock.VirtualNow + virtualElapsed;
        _timeProvider.Advance(TimeSpan.FromMilliseconds(Options.TickMilliseconds));
        return await _frames.Reader.ReadAsync(_cancellation.Token);
    }

    /// <summary>Delivers the completed frame directly to the waiting test.</summary>
    public Task publishAsync(SimulationFrame frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_frames.Writer.TryWrite(frame))
        {
            throw new InvalidOperationException("frame channel closed");
        }

        return Task.CompletedTask;
    }

    /// <summary>Stops the loop even when a test assertion fails.</summary>
    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        if (_runTask != null)
        {
            await _runTask;
        }

        _frames.Writer.TryComplete();
        _commands.Writer.TryComplete();
        _cancellation.Dispose();
    }
}
