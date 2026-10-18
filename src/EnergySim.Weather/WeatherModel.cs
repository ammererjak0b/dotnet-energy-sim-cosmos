using EnergySim.Core.Weather;


namespace EnergySim.Weather;

/// <summary>
/// Pure weather function f(locationId, timeUtc). No state, no Random: same input gives same output in any call order.
/// Lookup by (month, day, hour) of the reference year, the year of the query is ignored.
/// </summary>
public sealed class WeatherModel
{
    private const int REFERENCE_YEAR = 2025; // non leap year, 8760 rows
    private const int HOURS_PER_DAY = 24;

    private readonly IReadOnlyDictionary<string, WeatherDataSet> _dataSets;

    public WeatherModel(IReadOnlyDictionary<string, WeatherDataSet> dataSets)
    {
        if (dataSets == null)
        {
            throw new ArgumentNullException(nameof(dataSets));
        }
        _dataSets = dataSets;
    }

    /// <summary>Weather at the location and UTC time. Throws <see cref="UnknownLocationException"/> for unknown ids.</summary>
    public WeatherDto getWeather(string locationId, DateTimeOffset timeUtc)
    {
        Location location = LocationCatalog.get(locationId);
        if (!_dataSets.TryGetValue(location.Id, out WeatherDataSet? dataSet))
        {
            throw new WeatherDataException("No weather data loaded for location '" + location.Id + "'");
        }

        DateTime utc = timeUtc.UtcDateTime;
        int day = utc.Day;
        if (utc.Month == 2 && utc.Day == 29)
        {
            day = 28; // leap day -> 28 Feb
        }
        int dayOfYear = new DateTime(REFERENCE_YEAR, utc.Month, day).DayOfYear;
        int index = (dayOfYear - 1) * HOURS_PER_DAY + utc.Hour;
        int nextIndex = (index + 1) % WeatherDataSet.HOURS_PER_YEAR; // 31 Dec 23:00 -> 1 Jan 00:00
        double fraction = (utc.Ticks % TimeSpan.TicksPerHour) / (double)TimeSpan.TicksPerHour; // 0..1 within the hour

        WeatherDto weather = new WeatherDto();
        weather.LocationId = location.Id;
        weather.TimeUtc = timeUtc;
        weather.SunElevationDeg = SunPosition.elevationDeg(location.Latitude, location.Longitude, timeUtc);
        weather.SunAzimuthDeg = SunPosition.azimuthDeg(location.Latitude, location.Longitude, timeUtc);
        weather.TemperatureC = interpolate(dataSet.TemperatureC, index, nextIndex, fraction);
        weather.CloudCoverPct = interpolate(dataSet.CloudCoverPct, index, nextIndex, fraction);
        weather.GlobalIrradianceWm2 = interpolate(dataSet.ShortwaveRadiationWm2, index, nextIndex, fraction);
        weather.WindSpeedMs = interpolate(dataSet.WindSpeedMs, index, nextIndex, fraction);
        weather.PrecipitationMmPerH = dataSet.PrecipitationMmPerH[index]; // step value of the current hour
        weather.WindDirectionDeg = interpolateAngle(dataSet.WindDirectionDeg[index], dataSet.WindDirectionDeg[nextIndex], fraction);
        return weather;
    }

    private static double interpolate(double[] values, int index, int nextIndex, double fraction)
    {
        return values[index] + (values[nextIndex] - values[index]) * fraction;
    }

    // shortest way around the circle, result in 0..360
    private static double interpolateAngle(double fromDeg, double toDeg, double fraction)
    {
        double differenceDeg = ((toDeg - fromDeg + 540.0) % 360.0) - 180.0; // -180..180
        double resultDeg = (fromDeg + differenceDeg * fraction) % 360.0;
        if (resultDeg < 0)
        {
            resultDeg += 360.0;
        }
        return resultDeg;
    }
}
