namespace EnergySim.Weather;

/// <summary>Sun position from the NOAA solar calculator approximation. Accuracy well below 0.5 deg, no refraction.</summary>
public static class SunPosition
{
    private const double JULIAN_DAY_UNIX_EPOCH = 2440587.5;
    private const double JULIAN_DAY_J2000 = 2451545.0;
    private const double DAYS_PER_CENTURY = 36525.0;
    private const double MINUTES_PER_DAY = 1440.0;
    private const double DEG_TO_RAD = Math.PI / 180.0;
    private const double RAD_TO_DEG = 180.0 / Math.PI;

    /// <summary>Hour angle (deg, 0 at solar noon, afternoon positive) and declination (rad).</summary>
    private sealed class SunState
    {
        public double HourAngleDeg { get; set; }

        public double DeclinationRad { get; set; }
    }

    /// <summary>Elevation above the horizon in degrees (negative = night).</summary>
    public static double elevationDeg(double latitudeDeg, double longitudeDeg, DateTimeOffset timeUtc)
    {
        SunState state = calculateState(longitudeDeg, timeUtc);
        double cosZenith = calculateCosZenith(latitudeDeg, state);
        return 90.0 - Math.Acos(cosZenith) * RAD_TO_DEG;
    }

    /// <summary>Azimuth in degrees, clockwise from north (90 = east, 180 = south).</summary>
    public static double azimuthDeg(double latitudeDeg, double longitudeDeg, DateTimeOffset timeUtc)
    {
        SunState state = calculateState(longitudeDeg, timeUtc);
        double latitudeRad = latitudeDeg * DEG_TO_RAD;
        double cosZenith = calculateCosZenith(latitudeDeg, state);
        double sinZenith = Math.Sqrt(1.0 - cosZenith * cosZenith);
        if (sinZenith < 1e-9)
        {
            return 180.0; // sun in zenith, azimuth undefined
        }
        double cosAzimuth = (Math.Sin(latitudeRad) * cosZenith - Math.Sin(state.DeclinationRad)) / (Math.Cos(latitudeRad) * sinZenith);
        cosAzimuth = Math.Clamp(cosAzimuth, -1.0, 1.0);
        double angleDeg = Math.Acos(cosAzimuth) * RAD_TO_DEG;
        if (state.HourAngleDeg > 0)
        {
            return (angleDeg + 180.0) % 360.0; // afternoon: west side
        }
        return (540.0 - angleDeg) % 360.0; // morning: east side
    }

    private static double calculateCosZenith(double latitudeDeg, SunState state)
    {
        double latitudeRad = latitudeDeg * DEG_TO_RAD;
        double hourAngleRad = state.HourAngleDeg * DEG_TO_RAD;
        double cosZenith = Math.Sin(latitudeRad) * Math.Sin(state.DeclinationRad)
            + Math.Cos(latitudeRad) * Math.Cos(state.DeclinationRad) * Math.Cos(hourAngleRad);
        return Math.Clamp(cosZenith, -1.0, 1.0);
    }

    private static SunState calculateState(double longitudeDeg, DateTimeOffset timeUtc)
    {
        DateTime utc = timeUtc.UtcDateTime;
        double julianDay = (utc - DateTime.UnixEpoch).TotalDays + JULIAN_DAY_UNIX_EPOCH;
        double t = (julianDay - JULIAN_DAY_J2000) / DAYS_PER_CENTURY; // julian centuries since J2000

        double meanLongitudeDeg = (280.46646 + t * (36000.76983 + t * 0.0003032)) % 360.0;
        double meanAnomalyDeg = 357.52911 + t * (35999.05029 - 0.0001537 * t);
        double eccentricity = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
        double meanAnomalyRad = meanAnomalyDeg * DEG_TO_RAD;

        double equationOfCenterDeg = Math.Sin(meanAnomalyRad) * (1.914602 - t * (0.004817 + 0.000014 * t))
            + Math.Sin(2 * meanAnomalyRad) * (0.019993 - 0.000101 * t)
            + Math.Sin(3 * meanAnomalyRad) * 0.000289;
        double trueLongitudeDeg = meanLongitudeDeg + equationOfCenterDeg;
        double omegaDeg = 125.04 - 1934.136 * t;
        double apparentLongitudeDeg = trueLongitudeDeg - 0.00569 - 0.00478 * Math.Sin(omegaDeg * DEG_TO_RAD);

        double meanObliquityDeg = 23.0 + (26.0 + (21.448 - t * (46.815 + t * (0.00059 - t * 0.001813))) / 60.0) / 60.0;
        double obliquityDeg = meanObliquityDeg + 0.00256 * Math.Cos(omegaDeg * DEG_TO_RAD);
        double declinationRad = Math.Asin(Math.Sin(obliquityDeg * DEG_TO_RAD) * Math.Sin(apparentLongitudeDeg * DEG_TO_RAD));

        double y = Math.Tan(obliquityDeg * DEG_TO_RAD / 2.0);
        y = y * y;
        double meanLongitudeRad = meanLongitudeDeg * DEG_TO_RAD;
        double equationOfTimeMinutes = 4.0 * RAD_TO_DEG * (y * Math.Sin(2 * meanLongitudeRad)
            - 2 * eccentricity * Math.Sin(meanAnomalyRad)
            + 4 * eccentricity * y * Math.Sin(meanAnomalyRad) * Math.Cos(2 * meanLongitudeRad)
            - 0.5 * y * y * Math.Sin(4 * meanLongitudeRad)
            - 1.25 * eccentricity * eccentricity * Math.Sin(2 * meanAnomalyRad));

        double minutesOfDayUtc = utc.TimeOfDay.TotalMinutes;
        double trueSolarTimeMinutes = (minutesOfDayUtc + equationOfTimeMinutes + 4.0 * longitudeDeg) % MINUTES_PER_DAY; // 4 min per deg longitude
        if (trueSolarTimeMinutes < 0)
        {
            trueSolarTimeMinutes += MINUTES_PER_DAY;
        }

        SunState state = new SunState();
        state.HourAngleDeg = trueSolarTimeMinutes / 4.0 - 180.0;
        state.DeclinationRad = declinationRad;
        return state;
    }
}
