namespace EnergySim.Core.Engine;

/// <summary>What a simulator knows about the virtual clock right now.</summary>
public sealed class ClockSnapshot
{
    public Guid SessionId { get; set; }

    public int Revision { get; set; }

    public string SyncError { get; set; } = string.Empty;

    public DateTimeOffset VirtualNow { get; set; }

    public ClockStatus Status { get; set; } = ClockStatus.Stopped;

    public double Speed { get; set; } = 1.0;

    public int Generation { get; set; }

    public bool IsSynced { get; set; } // current service connection and timeline are valid

    public bool IsUsingFallback { get; set; } // last known settings while the service is unavailable
}
