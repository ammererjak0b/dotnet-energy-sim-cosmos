using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

/// <summary>Settable clock view. Tests move VirtualNow by hand to mimic SimClock readings.</summary>
public sealed class FakeClockSource : IClockSource
{
    private readonly object _lock = new object();

    public FakeClockSource()
    {
        VirtualNow = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Status = ClockStatus.Running;
        Speed = 1.0;
        Generation = 1;
        IsSynced = true;
    }

    public DateTimeOffset VirtualNow { get; set; }

    public ClockStatus Status { get; set; }

    public double Speed { get; set; }

    public int Generation { get; set; }

    public bool IsSynced { get; set; }


    /// <summary>What a real clock would show after realElapsed: virtual time only moves while running.</summary>
    public void advance(TimeSpan realElapsed)
    {
        lock (_lock)
        {
            if (Status == ClockStatus.Running)
            {
                VirtualNow = VirtualNow + realElapsed * Speed;
            }
        }
    }

    public ClockSnapshot getSnapshot()
    {
        lock (_lock)
        {
            ClockSnapshot snapshot = new ClockSnapshot();
            snapshot.VirtualNow = VirtualNow;
            snapshot.Status = Status;
            snapshot.Speed = Speed;
            snapshot.Generation = Generation;
            snapshot.IsSynced = IsSynced;
            return snapshot;
        }
    }
}
