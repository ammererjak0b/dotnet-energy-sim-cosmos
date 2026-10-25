namespace EnergySim.Modbus;

/// <summary>Compact liveness message using the service monotonic counter.</summary>
public sealed class ClockHeartbeat
{
    public Guid SessionId { get; set; }
    public int Revision { get; set; }
    public long IssuedTimestamp { get; set; }
}
