using System.Globalization;

namespace EnergySim.Terminal;

/// <summary>Virtual time for humans: local Vienna time with zone abbreviation plus the UTC time.</summary>
public static class VirtualTimeFormat
{
    public const string DEFAULT_ZONE_ID = "Europe/Vienna";

    private const string WINDOWS_VIENNA_ZONE_ID = "W. Europe Standard Time"; // when no IANA db

    private const string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm";

    private const string TIME_FORMAT = "HH:mm";

    /// <summary>Example: 2026-06-21 06:00 CEST (04:00Z).</summary>
    public static string format(DateTimeOffset utc)
    {
        return format(utc, DEFAULT_ZONE_ID);
    }

    public static string format(DateTimeOffset utc, string zoneId)
    {
        DateTimeOffset utcTime = utc.ToUniversalTime();
        string utcText = utcTime.ToString(TIME_FORMAT, CultureInfo.InvariantCulture) + "Z";
        TimeZoneInfo? zone = findZone(zoneId);
        if (zone == null)
        {
            return utcTime.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture) + " UTC (no tz data) (" + utcText + ")";
        }

        DateTimeOffset local = TimeZoneInfo.ConvertTime(utcTime, zone);
        string abbreviation = getAbbreviation(zoneId, zone, local);
        return local.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture) + " " + abbreviation + " (" + utcText + ")";
    }

    private static TimeZoneInfo? findZone(string zoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // try windows id below
        }
        catch (InvalidTimeZoneException)
        {
            return null; // corrupt db entry
        }

        if (zoneId != DEFAULT_ZONE_ID)
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WINDOWS_VIENNA_ZONE_ID);
        }
        catch (TimeZoneNotFoundException)
        {
            return null; // no tz database at all
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static string getAbbreviation(string zoneId, TimeZoneInfo zone, DateTimeOffset local)
    {
        if (zoneId != DEFAULT_ZONE_ID)
        {
            return zoneId; // .NET has no abbreviations, id is the honest label
        }

        if (zone.IsDaylightSavingTime(local))
        {
            return "CEST";
        }

        return "CET";
    }
}
