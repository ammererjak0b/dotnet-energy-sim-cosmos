namespace EnergySim.Time;

/// <summary>Control operations accepted by the time authority.</summary>
public enum ClockAction
{
    Start,
    Pause,
    Resume,
    SetSpeed,
    Restart
}
