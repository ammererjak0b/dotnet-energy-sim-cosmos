namespace EnergySim.Weather;

/// <summary>Contract of the weather process: route, query names and time format. Shared by HttpWeatherClient and the weather exe.</summary>
public static class WeatherApi
{
    public const string ROUTE = "/api/weather";

    public const string QUERY_LOCATION = "location";

    public const string QUERY_TIME = "time";

    public const string TIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ss'Z'"; // UTC only, whole seconds

    public const int DEFAULT_PORT = 5200;
}
