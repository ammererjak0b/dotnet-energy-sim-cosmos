using EnergySim.Core.Devices;
using Spectre.Console;

namespace EnergySim.Terminal;

/// <summary>The one place that decides colors: green ok, yellow warning, red fault.</summary>
public static class TerminalStyle
{
    public static Color colorFor(DisplaySeverity severity)
    {
        if (severity == DisplaySeverity.Fault)
        {
            return Color.Red;
        }

        if (severity == DisplaySeverity.Warning)
        {
            return Color.Yellow;
        }

        return Color.Green;
    }

    public static Style styleFor(DisplaySeverity severity)
    {
        return new Style(colorFor(severity));
    }
}
