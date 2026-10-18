using System.Globalization;
using Microsoft.Extensions.Logging;

namespace EnergySim.Terminal;

/// <summary>One line format for dashboard log and headless output: HH:mm:ss LVL message (UTC).</summary>
public static class LogLineFormat
{
    private const string TIME_FORMAT = "HH:mm:ss";

    public static string format(DateTimeOffset utcNow, LogLevel level, string message)
    {
        string time = utcNow.ToUniversalTime().ToString(TIME_FORMAT, CultureInfo.InvariantCulture);
        return time + " " + getLevelText(level) + " " + message;
    }

    private static string getLevelText(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
                return "TRC";
            case LogLevel.Debug:
                return "DBG";
            case LogLevel.Information:
                return "INF";
            case LogLevel.Warning:
                return "WRN";
            case LogLevel.Error:
                return "ERR";
            case LogLevel.Critical:
                return "CRT";
            default:
                return "???";
        }
    }
}
