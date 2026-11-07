namespace EnergySim.Time;

/// <summary>Registered simulator endpoint shown by the service dashboard.</summary>
public sealed class ClockParticipantView
{
    public string DeviceId { get; set; } = string.Empty;
    public int Port { get; set; }
    public ClockParticipantStatus Status { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; } // registration time while status is New
    public int MissedHeartbeats { get; set; }
    public bool HasSkew { get; set; } // false until the device reported its virtual time
    public double SkewMilliseconds { get; set; } // device virtual time minus master virtual time
}
