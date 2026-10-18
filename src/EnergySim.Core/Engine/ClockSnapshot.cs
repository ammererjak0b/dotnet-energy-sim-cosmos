namespace EnergySim.Core.Engine;

/// <summary>What a simulator knows about the virtual clock right now.</summary>
public sealed class ClockSnapshot
{
    public DateTimeOffset VirtualNow { get; set; }

    public ClockStatus Status { get; set; } = ClockStatus.Stopped;

    public double Speed { get; set; } = 1.0;

    public int Generation { get; set; }

    public bool IsSynced { get; set; } // false until the first state was received (always true for LocalClock)
}
