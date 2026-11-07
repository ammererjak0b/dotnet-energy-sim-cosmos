namespace EnergySim.Time;

/// <summary>Liveness of a registered simulator as seen by the master.</summary>
public enum ClockParticipantStatus
{
    New, // registered, no heartbeat delivered yet
    Ok, // last heartbeat delivered
    Unreachable // consecutive heartbeats missed, still retried
}
