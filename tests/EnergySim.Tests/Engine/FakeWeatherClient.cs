using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Engine;

/// <summary>Returns a configurable result and counts calls.</summary>
public sealed class FakeWeatherClient : IWeatherClient
{
    private int _callCount;

    public FakeWeatherClient()
    {
        Result = new WeatherResult();
        WeatherDto weather = new WeatherDto();
        weather.LocationId = "salzburg";
        weather.TemperatureC = 18.5;
        weather.CloudCoverPct = 40;
        weather.WindSpeedMs = 5.0;
        Result.Weather = weather;
    }

    public WeatherResult Result { get; set; }

    public int CallCount
    {
        get
        {
            return Volatile.Read(ref _callCount);
        }
    }

    public string LastLocationId { get; private set; } = string.Empty;

    public Task<WeatherResult> getWeatherAsync(string locationId, DateTimeOffset virtualTimeUtc, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastLocationId = locationId;
        return Task.FromResult(Result);
    }
}
