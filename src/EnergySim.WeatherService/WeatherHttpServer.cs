using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Web;
using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.WeatherService;

/// <summary>HTTP front of WeatherModel: GET /api/weather?location=&amp;time=. No state, no clock: the caller sends the virtual time.</summary>
public sealed class WeatherHttpServer
{
    private const string CONTENT_TYPE_JSON = "application/json";
    private const string CONTENT_TYPE_TEXT = "text/plain";

    private readonly WeatherModel _model;
    private readonly int _port;
    private readonly TextWriter _log;

    private string _lastLoggedKey = string.Empty;
    private int _requestsSinceLastLog;

    public WeatherHttpServer(WeatherModel model, int port, TextWriter log)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (log == null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        _model = model;
        _port = port;
        _log = log;
    }

    /// <summary>Serves until the token is cancelled. Throws HttpListenerException when the port is taken.</summary>
    public async Task runAsync(CancellationToken cancellationToken)
    {
        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://+:" + _port + "/");
        listener.Start();
        using CancellationTokenRegistration registration = cancellationToken.Register(listener.Stop); // GetContextAsync has no token
        await _log.WriteLineAsync("weather service listening on port " + _port + ", locations: " + string.Join(", ", LocationCatalog.ids()));
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break; // listener stopped by Ctrl+C
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await handleAsync(context, cancellationToken);
        }

        await _log.WriteLineAsync("weather service stopped");
    }

    private async Task handleAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (context.Request.HttpMethod != "GET" || context.Request.Url == null || context.Request.Url.AbsolutePath != WeatherApi.ROUTE)
            {
                await writeTextAsync(context.Response, HttpStatusCode.NotFound, "not found, use GET " + WeatherApi.ROUTE, cancellationToken);
                return;
            }

            await handleWeatherAsync(context, context.Request.Url.Query, cancellationToken);
        }
        catch (HttpListenerException exception)
        {
            await _log.WriteLineAsync("client connection lost: " + exception.Message); // client left, nothing to answer
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task handleWeatherAsync(HttpListenerContext context, string queryText, CancellationToken cancellationToken)
    {
        System.Collections.Specialized.NameValueCollection query = HttpUtility.ParseQueryString(queryText);
        string? locationId = query[WeatherApi.QUERY_LOCATION];
        string? timeText = query[WeatherApi.QUERY_TIME];
        if (string.IsNullOrWhiteSpace(locationId))
        {
            await writeTextAsync(context.Response, HttpStatusCode.BadRequest, "missing query '" + WeatherApi.QUERY_LOCATION + "'", cancellationToken);
            return;
        }

        DateTimeOffset timeUtc;
        if (string.IsNullOrWhiteSpace(timeText) || !DateTimeOffset.TryParseExact(timeText, WeatherApi.TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timeUtc))
        {
            await writeTextAsync(context.Response, HttpStatusCode.BadRequest, "query '" + WeatherApi.QUERY_TIME + "' must be UTC like 2025-07-01T12:00:00Z", cancellationToken);
            return;
        }

        WeatherDto weather;
        try
        {
            weather = _model.getWeather(locationId.ToLowerInvariant(), timeUtc);
        }
        catch (UnknownLocationException exception)
        {
            await writeTextAsync(context.Response, HttpStatusCode.NotFound, exception.Message, cancellationToken);
            return;
        }
        catch (WeatherDataException exception)
        {
            await _log.WriteLineAsync("data error: " + exception.Message);
            await writeTextAsync(context.Response, HttpStatusCode.InternalServerError, exception.Message, cancellationToken);
            return;
        }

        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = CONTENT_TYPE_JSON;
        await JsonSerializer.SerializeAsync(context.Response.OutputStream, weather, cancellationToken: cancellationToken);
        await logQueryAsync(weather);
    }

    /// <summary>One line per new (location, minute), not per request: simulators ask 5 times per second.</summary>
    private async Task logQueryAsync(WeatherDto weather)
    {
        string key = weather.LocationId + " " + weather.TimeUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        _requestsSinceLastLog++;
        if (key == _lastLoggedKey)
        {
            return;
        }

        string line = key + " => T=" + format(weather.TemperatureC) + "C cloud=" + format(weather.CloudCoverPct) + "% G=" + format(weather.GlobalIrradianceWm2) + "W/m2 sun=" + format(weather.SunElevationDeg) + "deg wind=" + format(weather.WindSpeedMs) + "m/s rain=" + format(weather.PrecipitationMmPerH) + "mm/h (" + _requestsSinceLastLog + " requests)";
        _lastLoggedKey = key;
        _requestsSinceLastLog = 0;
        await _log.WriteLineAsync(line);
    }

    private static string format(double value)
    {
        return value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static async Task writeTextAsync(HttpListenerResponse response, HttpStatusCode statusCode, string text, CancellationToken cancellationToken)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = CONTENT_TYPE_TEXT;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
    }
}
