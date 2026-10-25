using System.Globalization;
using System.Text.Json;
using EnergySim.Core.Weather;

namespace EnergySim.Weather;

/// <summary>IWeatherClient that asks the weather process over HTTP. Time is floored to the minute so all simulators get the same values.</summary>
public sealed class HttpWeatherClient : IWeatherClient
{
    private const int REQUEST_TIMEOUT_SECONDS = 2;

    private static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly Dictionary<string, WeatherDto> _lastGoodByLocation = new Dictionary<string, WeatherDto>();

    public HttpWeatherClient(HttpClient httpClient, string baseUrl)
    {
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("baseUrl is empty", nameof(baseUrl));
        }

        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<WeatherResult> getWeatherAsync(string locationId, DateTimeOffset virtualTimeUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string url = buildUrl(locationId, floorToMinute(virtualTimeUtc));
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS));
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(timeout.Token);
                return fail(locationId, "weather service answered " + (int)response.StatusCode + ": " + body);
            }

            using Stream stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            WeatherDto? weather = await JsonSerializer.DeserializeAsync<WeatherDto>(stream, JSON_OPTIONS, timeout.Token);
            if (weather == null)
            {
                return fail(locationId, "weather service answered with empty body");
            }

            _lastGoodByLocation[locationId] = weather;
            WeatherResult result = new WeatherResult();
            result.Weather = weather;
            return result;
        }
        catch (HttpRequestException exception)
        {
            return fail(locationId, "weather service unreachable: " + exception.Message);
        }
        catch (JsonException exception)
        {
            return fail(locationId, "weather service answer invalid: " + exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return fail(locationId, "weather service timeout after " + REQUEST_TIMEOUT_SECONDS + " s"); // caller not cancelled = our timeout
        }
    }

    /// <summary>Last known good value flagged stale, or no weather when there never was one.</summary>
    private WeatherResult fail(string locationId, string errorMessage)
    {
        WeatherResult result = new WeatherResult();
        result.ErrorMessage = errorMessage;
        WeatherDto? lastGood;
        if (_lastGoodByLocation.TryGetValue(locationId, out lastGood))
        {
            result.Weather = lastGood;
            result.IsStale = true;
        }

        return result;
    }

    private string buildUrl(string locationId, DateTimeOffset queryTimeUtc)
    {
        string timeText = queryTimeUtc.ToString(WeatherApi.TIME_FORMAT, CultureInfo.InvariantCulture);
        return _baseUrl + WeatherApi.ROUTE + "?" + WeatherApi.QUERY_LOCATION + "=" + Uri.EscapeDataString(locationId) + "&" + WeatherApi.QUERY_TIME + "=" + Uri.EscapeDataString(timeText);
    }

    /// <summary>Same minute = same query = same values, whatever the tick of the caller.</summary>
    public static DateTimeOffset floorToMinute(DateTimeOffset timeUtc)
    {
        DateTimeOffset utc = timeUtc.ToUniversalTime();
        return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMinute, TimeSpan.Zero);
    }
}
