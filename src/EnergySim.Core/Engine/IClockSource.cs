namespace EnergySim.Core.Engine;

/// <summary>Local view on the virtual clock. Implemented by LocalClock (later also by a synced clock), consumed by the loop.</summary>
public interface IClockSource
{
    ClockSnapshot getSnapshot();

    /// <summary>Simple clocks publish one sample; timeline clocks also publish crossed boundaries.</summary>
    IReadOnlyList<ClockSnapshot> readAdvance(ClockCursor cursor)
    {
        return new List<ClockSnapshot> { getSnapshot() };
    }
}
