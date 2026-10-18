using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

/// <summary>Records every published frame (also the ones it throws on).</summary>
public sealed class FakeSimulationSink : ISimulationSink
{
    private readonly object _lock = new object();

    private readonly List<SimulationFrame> _frames = new List<SimulationFrame>();

    private bool _throwOnPublish;

    public bool ThrowOnPublish
    {
        get
        {
            lock (_lock)
            {
                return _throwOnPublish;
            }
        }
        set
        {
            lock (_lock)
            {
                _throwOnPublish = value;
            }
        }
    }

    public int FrameCount
    {
        get
        {
            lock (_lock)
            {
                return _frames.Count;
            }
        }
    }

    public SimulationFrame LastFrame
    {
        get
        {
            lock (_lock)
            {
                return _frames[_frames.Count - 1];
            }
        }
    }

    public Task publishAsync(SimulationFrame frame, CancellationToken ct)
    {
        lock (_lock)
        {
            _frames.Add(frame);
            if (_throwOnPublish)
            {
                throw new InvalidOperationException("sink broken");
            }
        }

        return Task.CompletedTask;
    }
}
