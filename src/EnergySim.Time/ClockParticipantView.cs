namespace EnergySim.Time;

/// <summary>Registered simulator endpoint shown by the service dashboard.</summary>
public sealed class ClockParticipantView
{
    public string DeviceId { get; set; } = string.Empty;
    public int Port { get; set; }
}
