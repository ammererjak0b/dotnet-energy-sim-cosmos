namespace EnergySim.Messaging;

/// <summary>One device state at one simulation time. Contract: docs/contracts/messaging.md.</summary>
public sealed class TelemetryMessage
{
    public const int SCHEMA_VERSION = 1;

    public int SchemaVersion { get; set; } = SCHEMA_VERSION;

    public string DeviceId { get; set; } = string.Empty;

    public string DeviceType { get; set; } = string.Empty;

    public DateTime? SimTimeUtc { get; set; } // virtual time of the state, from the register, null when quality is Bad

    public DateTime PolledAtUtc { get; set; } // real time of the poll, never mixed with SimTimeUtc

    public uint Sequence { get; set; }

    public ushort Generation { get; set; }

    public string ClockState { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public ushort FaultCode { get; set; }

    public List<string> WarnFlags { get; set; } = new List<string>();

    public TelemetryQuality Quality { get; set; }

    public List<Measurement> Measurements { get; set; } = new List<Measurement>();

    public string Error { get; set; } = string.Empty; // reason when quality is Bad
}
