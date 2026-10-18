using EnergySim.Core.Devices;

namespace EnergySim.Terminal;

/// <summary>Snapshot the device screen draws. The source fills it, the dashboard only reads it.</summary>
public sealed class DashboardState
{
    public string DeviceId { get; set; } = string.Empty;

    public string DeviceTypeName { get; set; } = string.Empty;

    public string LocationId { get; set; } = string.Empty;

    public string ClockText { get; set; } = string.Empty; // see VirtualTimeFormat

    public double Speed { get; set; }

    public string ClockStatus { get; set; } = string.Empty;

    public string WarningText { get; set; } = string.Empty;

    public string BannerText { get; set; } = string.Empty; // empty = no banner

    public DisplaySeverity BannerSeverity { get; set; } = DisplaySeverity.Warning;

    public List<DisplayRow> HeaderRows { get; } = new List<DisplayRow>(); // power, energy, state, ...

    public List<DisplayRow> DeviceRows { get; } = new List<DisplayRow>(); // ISimulatedDevice.DisplayRows

    public List<string> LogLines { get; } = new List<string>();

    public List<CommandDescriptor> Commands { get; } = new List<CommandDescriptor>();
}
