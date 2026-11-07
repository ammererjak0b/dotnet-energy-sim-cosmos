using System.Globalization;
using EnergySim.Time;

namespace EnergySim.Time.Cli;

/// <summary>One text form of a participant status, shared by dashboard and headless table.</summary>
public static class ParticipantFormat
{
    private const string SEEN_FORMAT = "HH:mm:ss";

    public static string formatStatus(ClockParticipantView view)
    {
        string statusText = "OK";
        if (view.Status == ClockParticipantStatus.New)
        {
            statusText = "NEW";
        }
        else if (view.Status == ClockParticipantStatus.Unreachable)
        {
            statusText = "UNREACHABLE";
        }

        string skewText = "-"; // device did not report its virtual time
        if (view.HasSkew)
        {
            skewText = view.SkewMilliseconds.ToString("+0;-0;0", CultureInfo.InvariantCulture) + " ms";
        }

        string seenText = view.LastSeenUtc.ToString(SEEN_FORMAT, CultureInfo.InvariantCulture) + "Z";
        return statusText + " | last seen " + seenText + " | missed " + view.MissedHeartbeats + " | skew " + skewText;
    }

    public static string formatLabel(ClockParticipantView view)
    {
        return view.DeviceId + " @ " + view.Port;
    }
}
