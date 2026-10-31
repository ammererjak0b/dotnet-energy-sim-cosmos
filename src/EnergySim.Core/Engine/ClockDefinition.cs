namespace EnergySim.Core.Engine;

/// <summary>Versioned mapping from a host monotonic counter to virtual UTC.</summary>
public sealed class ClockDefinition
{
    public Guid SessionId { get; set; }
    public int Generation { get; set; } = 1;
    public int Revision { get; set; } = 1;
    public long CounterFrequency { get; set; }
    public long AnchorTimestamp { get; set; }
    public DateTimeOffset VirtualAnchor { get; set; }
    public double Speed { get; set; } = 60;
    public ClockStatus Status { get; set; } = ClockStatus.Stopped;

    /// <summary>Copies a value before crossing a thread or ownership boundary.</summary>
    public ClockDefinition copy()
    {
        return (ClockDefinition)MemberwiseClone();
    }

    /// <summary>Rejects incomplete or unrepresentable clock definitions.</summary>
    public void validate()
    {
        if (SessionId == Guid.Empty || Generation < 1 || Revision < 1 || CounterFrequency <= 0)
        {
            throw new ArgumentException("Invalid clock identity or counter frequency.");
        }

        if (!double.IsFinite(Speed) || Speed < LocalClock.MIN_SPEED || Speed > LocalClock.MAX_SPEED)
        {
            throw new ArgumentException("Invalid clock speed.");
        }

        if (!Enum.IsDefined(Status))
        {
            throw new ArgumentException("Invalid clock state.");
        }
    }

    /// <summary>Evaluates elapsed counter ticks, never wall-clock subtraction.</summary>
    public ClockSnapshot getSnapshot(long timestamp)
    {
        DateTimeOffset virtualNow = VirtualAnchor;
        if (Status == ClockStatus.Running)
        {
            double elapsedSeconds = (timestamp - AnchorTimestamp) / (double)CounterFrequency;
            virtualNow += TimeSpan.FromSeconds(elapsedSeconds * Speed);
        }

        ClockSnapshot snapshot = new ClockSnapshot();
        snapshot.VirtualNow = virtualNow;
        snapshot.Speed = Speed;
        snapshot.Status = Status;
        snapshot.Generation = Generation;
        snapshot.IsSynced = true;
        snapshot.SessionId = SessionId;
        snapshot.Revision = Revision;
        return snapshot;
    }
}
