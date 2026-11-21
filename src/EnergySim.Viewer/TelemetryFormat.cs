using System.Globalization;
using EnergySim.Messaging;

namespace EnergySim.Viewer;

/// <summary>Text of one telemetry message for the console. Pure, no console access.</summary>
public static class TelemetryFormat
{
    private const string SIM_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss'Z'";
    private const string VALUE_FORMAT = "0.##";

    public static string format(TelemetryMessage message)
    {
        string simTime = "-"; // Bad has no virtual time
        if (message.SimTimeUtc != null)
        {
            simTime = message.SimTimeUtc.Value.ToString(SIM_TIME_FORMAT, CultureInfo.InvariantCulture);
        }

        string mark = string.Empty;
        if (message.Quality == TelemetryQuality.Bad)
        {
            mark = "  !! BAD: " + message.Error;
        }

        if (message.Quality == TelemetryQuality.Uncertain)
        {
            mark = "  ! UNCERTAIN: " + string.Join(", ", message.WarnFlags) + " clock " + message.ClockState;
        }

        List<string> lines = new List<string>();
        lines.Add("[" + message.DeviceId + "] sim " + simTime + "  seq " + message.Sequence + "  " + message.Quality + "  " + message.State + mark);
        foreach (Measurement measurement in message.Measurements)
        {
            lines.Add("    " + measurement.Name + " = " + measurement.Value.ToString(VALUE_FORMAT, CultureInfo.InvariantCulture) + " " + measurement.Unit);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
