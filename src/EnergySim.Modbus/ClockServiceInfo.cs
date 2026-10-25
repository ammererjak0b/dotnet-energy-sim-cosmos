namespace EnergySim.Modbus;

/// <summary>Timing policy published by the time service to connecting devices.</summary>
public sealed class ClockServiceInfo
{
    public int HeartbeatMilliseconds { get; set; }
    public int HeartbeatExpirySeconds { get; set; }
    public int LeadTimeMilliseconds { get; set; }
}
