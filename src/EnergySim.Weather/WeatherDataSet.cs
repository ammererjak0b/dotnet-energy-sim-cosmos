namespace EnergySim.Weather;

/// <summary>One location, one reference year, hourly values (index 0 = 1 Jan 00:00 UTC). Wind in m/s.</summary>
public sealed class WeatherDataSet
{
    public const int HOURS_PER_YEAR = 8760; // 365 * 24, leap day not needed

    public WeatherDataSet(double[] temperatureC, double[] cloudCoverPct, double[] shortwaveRadiationWm2,
        double[] precipitationMmPerH, double[] windSpeedMs, double[] windDirectionDeg)
    {
        checkLength(temperatureC, "temperature_2m");
        checkLength(cloudCoverPct, "cloud_cover");
        checkLength(shortwaveRadiationWm2, "shortwave_radiation");
        checkLength(precipitationMmPerH, "precipitation");
        checkLength(windSpeedMs, "wind_speed_10m");
        checkLength(windDirectionDeg, "wind_direction_10m");
        TemperatureC = temperatureC;
        CloudCoverPct = cloudCoverPct;
        ShortwaveRadiationWm2 = shortwaveRadiationWm2;
        PrecipitationMmPerH = precipitationMmPerH;
        WindSpeedMs = windSpeedMs;
        WindDirectionDeg = windDirectionDeg;
    }

    public double[] TemperatureC { get; }

    public double[] CloudCoverPct { get; }

    public double[] ShortwaveRadiationWm2 { get; }

    public double[] PrecipitationMmPerH { get; }

    public double[] WindSpeedMs { get; }

    public double[] WindDirectionDeg { get; }

    private static void checkLength(double[] values, string fieldName)
    {
        if (values == null || values.Length != HOURS_PER_YEAR)
        {
            int actual = 0;
            if (values != null)
            {
                actual = values.Length;
            }
            throw new WeatherDataException("Field '" + fieldName + "' has " + actual + " hourly rows, expected " + HOURS_PER_YEAR);
        }
    }
}
