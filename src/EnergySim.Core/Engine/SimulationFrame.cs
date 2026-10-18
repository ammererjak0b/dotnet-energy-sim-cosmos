using EnergySim.Core.Devices;

namespace EnergySim.Core.Engine;

/// <summary>Device state after one tick. Header and lists are copies, safe to read from other threads.</summary>
public sealed class SimulationFrame
{
    public DateTimeOffset VirtualNow { get; set; }

    public ClockSnapshot Snapshot { get; set; } = new ClockSnapshot();

    public DeviceHeader Header { get; set; } = new DeviceHeader();

    public IReadOnlyList<DisplayRow> DisplayRows { get; set; } = new List<DisplayRow>();

    public uint Sequence { get; set; } // +1 per new device state, constant while paused

    public bool WeatherStale { get; set; } // last weather was old or missing

    public string WeatherInfo { get; set; } = string.Empty; // short dashboard text, last weather or error

    public string LastCommandMessage { get; set; } = string.Empty;

    public string DeviceError { get; set; } = string.Empty; // empty = device model ok
}
