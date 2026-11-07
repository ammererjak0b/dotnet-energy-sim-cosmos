namespace EnergySim.Modbus;

/// <summary>Value of the clock state register of the device header.</summary>
public enum ClockStateCode
{
    Stopped = 0,
    Running = 1,
    Paused = 2,
    Unsynced = 3 // no valid master clock yet
}
