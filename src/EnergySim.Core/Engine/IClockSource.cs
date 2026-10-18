namespace EnergySim.Core.Engine;

/// <summary>Local view on the virtual clock. Implemented by LocalClock (later also by a synced clock), consumed by the loop.</summary>
public interface IClockSource
{
    ClockSnapshot getSnapshot();
}
