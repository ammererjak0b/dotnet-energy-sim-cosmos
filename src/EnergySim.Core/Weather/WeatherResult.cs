namespace EnergySim.Core.Weather;

/// <summary>Answer of IWeatherClient. Weather is null only when no valid value was ever received.</summary>
public sealed class WeatherResult
{
    public WeatherDto? Weather { get; set; }

    public bool IsStale { get; set; } // true when the last known good value is returned

    public string ErrorMessage { get; set; } = string.Empty;
}
