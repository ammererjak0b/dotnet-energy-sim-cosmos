using System.Net;
using System.Text.Json;
using EnergySim.Tests.Support;
using EnergySim.Weather;
using EnergySim.WeatherService;

namespace EnergySim.Tests.WeatherService;

/// <summary>Real HttpListener on a loopback port, in-memory weather data.</summary>
public sealed class WeatherHttpServerTests
{
    private static readonly string[] REQUIRED_FIELDS = { "SunElevationDeg", "CloudCoverPct", "TemperatureC", "PrecipitationMmPerH", "WindSpeedMs" };

    private static async Task<ServerHandle> startServerAsync()
    {
        ServerHandle handle = new ServerHandle();
        await handle.startAsync();
        return handle;
    }

    [Fact]
    public async Task Get_ValidQuery_Returns200WithAllRequiredFields()
    {
        // Arrange
        await using ServerHandle server = await startServerAsync();

        // Act
        HttpResponseMessage response = await server.getAsync("location=salzburg&time=2025-07-01T12:00:00Z");
        string body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(body);
        foreach (string field in REQUIRED_FIELDS)
        {
            Assert.True(json.RootElement.TryGetProperty(field, out JsonElement value), "missing field " + field);
            Assert.Equal(JsonValueKind.Number, value.ValueKind);
        }

        Assert.Equal("salzburg", json.RootElement.GetProperty("LocationId").GetString());
    }

    [Theory]
    [InlineData("location=salzburg&time=yesterday")]
    [InlineData("location=salzburg&time=2025-07-01 12:00")]
    [InlineData("location=salzburg&time=2025-07-01T12:00:00")]
    [InlineData("location=salzburg")]
    [InlineData("time=2025-07-01T12:00:00Z")]
    public async Task Get_BadOrMissingParameter_Returns400(string query)
    {
        // Arrange
        await using ServerHandle server = await startServerAsync();

        // Act
        HttpResponseMessage response = await server.getAsync(query);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownLocation_Returns404()
    {
        // Arrange
        await using ServerHandle server = await startServerAsync();

        // Act
        HttpResponseMessage response = await server.getAsync("location=mars&time=2025-07-01T12:00:00Z");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownRoute_Returns404()
    {
        // Arrange
        await using ServerHandle server = await startServerAsync();

        // Act
        HttpResponseMessage response = await server.getRawAsync("/api/other");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_SameQueryTwice_ReturnsIdenticalBody()
    {
        // Arrange
        await using ServerHandle server = await startServerAsync();

        // Act
        HttpResponseMessage first = await server.getAsync("location=salzburg&time=2025-07-01T12:00:00Z");
        HttpResponseMessage second = await server.getAsync("location=salzburg&time=2025-07-01T12:00:00Z");

        // Assert
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
    }

    private sealed class ServerHandle : IAsyncDisposable
    {
        private const int REQUEST_TIMEOUT_MILLISECONDS = 5000;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly StringWriter _logText = new StringWriter();
        private readonly TextWriter _log;
        private readonly HttpClient _client = new HttpClient();
        private readonly int _port;
        private Task? _runTask;

        public ServerHandle()
        {
            _port = TestPorts.next(TestPorts.WEATHER_FIRST, TestPorts.WEATHER_LAST);
            _log = TextWriter.Synchronized(_logText);
        }

        public async Task startAsync()
        {
            WeatherModel model = EnergySim.Tests.Weather.TestDataSets.createModel(EnergySim.Tests.Weather.TestDataSets.createRamp());
            WeatherHttpServer server = new WeatherHttpServer(model, _port, _log);
            _runTask = Task.Run(delegate { return server.runAsync(_shutdown.Token); });
            await TestWait.untilAsync(delegate { return isListening(); });
        }

        public Task<HttpResponseMessage> getAsync(string query)
        {
            return getRawAsync(WeatherApi.ROUTE + "?" + query);
        }

        public async Task<HttpResponseMessage> getRawAsync(string pathAndQuery)
        {
            using CancellationTokenSource timeout = new CancellationTokenSource(REQUEST_TIMEOUT_MILLISECONDS);
            return await _client.GetAsync("http://127.0.0.1:" + _port + pathAndQuery, timeout.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown.Cancel();
            if (_runTask != null)
            {
                try
                {
                    await _runTask;
                }
                catch (OperationCanceledException)
                {
                    // normal shutdown
                }
            }

            _client.Dispose();
            _shutdown.Dispose();
        }

        private bool isListening()
        {
            lock (_log)
            {
                return _logText.ToString().Contains("listening");
            }
        }
    }
}
