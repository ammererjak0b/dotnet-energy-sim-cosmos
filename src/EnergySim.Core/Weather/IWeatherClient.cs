namespace EnergySim.Core.Weather;

public interface IWeatherClient
{
    /// <summary>Never throws for network or protocol errors: returns the last known good value flagged as stale.</summary>
    Task<WeatherResult> getWeatherAsync(string locationId, DateTimeOffset virtualTimeUtc, CancellationToken cancellationToken);
}
