namespace EnergySim.Core.Weather;

/// <summary>Weather at one location and virtual time. All times UTC, units in the property names.</summary>
public sealed class WeatherDto
{
    public string LocationId { get; set; } = string.Empty;

    public DateTimeOffset TimeUtc { get; set; }

    public double SunElevationDeg { get; set; }

    public double SunAzimuthDeg { get; set; }

    public double CloudCoverPct { get; set; }

    public double TemperatureC { get; set; }

    public double PrecipitationMmPerH { get; set; }

    public double WindSpeedMs { get; set; }

    public double WindDirectionDeg { get; set; }

    public double GlobalIrradianceWm2 { get; set; }
}
