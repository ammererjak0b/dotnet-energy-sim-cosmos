namespace EnergySim.Modbus;

/// <summary>Purpose of a clock definition write.</summary>
public enum ClockMessageKind
{
    Initialize = 1,
    Change = 2,
    Restart = 3,
    Heartbeat = 4
}
