namespace EnergySim.Core.Engine;

/// <summary>Per-consumer position in a monotonic clock timeline.</summary>
public sealed class ClockCursor
{
    public bool HasValue { get; set; }
    public long Timestamp { get; set; }
}
