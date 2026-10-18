using EnergySim.Terminal;
using System.Globalization;
using EnergySim.Core.Devices;

namespace EnergySim.Cli;

/// <summary>One plain text line per second for headless mode.</summary>
public static class StatusLine
{
    public static string format(DashboardState state)
    {
        List<string> parts = new List<string>();
        parts.Add(state.DeviceId);
        parts.Add(state.ClockText);
        parts.Add("Clock " + state.ClockStatus + " x" + state.Speed.ToString("0.##", CultureInfo.InvariantCulture));
        foreach (DisplayRow row in state.HeaderRows)
        {
            parts.Add(row.Label + " " + row.Value);
        }

        return string.Join(" | ", parts);
    }
}
