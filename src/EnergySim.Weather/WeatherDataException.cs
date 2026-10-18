namespace EnergySim.Weather;

/// <summary>Weather data file is missing, unreadable or incomplete.</summary>
public sealed class WeatherDataException : Exception
{
    public WeatherDataException(string message)
        : base(message)
    {
    }

    public WeatherDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
