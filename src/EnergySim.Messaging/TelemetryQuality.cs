namespace EnergySim.Messaging;

/// <summary>Trust level of one message, see docs/contracts/messaging.md.</summary>
public enum TelemetryQuality
{
    Good,
    Uncertain,
    Bad
}
