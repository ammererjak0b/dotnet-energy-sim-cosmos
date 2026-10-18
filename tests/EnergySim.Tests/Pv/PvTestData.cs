using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Pv;

/// <summary>Weather samples shared by the PV tests.</summary>
internal static class PvTestData
{
    public static readonly DateTimeOffset NOON = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    public static readonly TimeSpan STEP = TimeSpan.FromMinutes(5);

    public static WeatherDto createWeather(double irradianceWm2, double temperatureC)
    {
        WeatherDto weather = new WeatherDto();
        weather.LocationId = "salzburg";
        weather.GlobalIrradianceWm2 = irradianceWm2;
        weather.TemperatureC = temperatureC;
        weather.SunElevationDeg = 60.0;
        weather.CloudCoverPct = 20.0;
        return weather;
    }

    public static WeatherDto createMiddayWeather()
    {
        return createWeather(800.0, 20.0); // cell 40 C -> derating 0.94 -> 3008 W at 5 kWp, PR 0.8
    }

    public static WeatherDto createNightWeather()
    {
        WeatherDto weather = createWeather(0.0, 10.0);
        weather.SunElevationDeg = -20.0;
        return weather;
    }
}
