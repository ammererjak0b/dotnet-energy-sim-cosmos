using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

/// <summary>Same location + same virtual time => same values, in any order. Weather process down => last good value, flagged stale.</summary>
public class WeatherReproducibilityTests
{
    private static readonly DateTimeOffset NOON = new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void GetWeather_SameQueryTwoModels_ReturnsSameValues()
    {
        // Arrange
        WeatherModel first = TestDataSets.createModel(TestDataSets.createRamp());
        WeatherModel second = TestDataSets.createModel(TestDataSets.createRamp());

        // Act
        WeatherDto fromFirst = first.getWeather(TestDataSets.LOCATION_ID, NOON);
        WeatherDto fromSecond = second.getWeather(TestDataSets.LOCATION_ID, NOON);

        // Assert
        assertSameValues(fromFirst, fromSecond);
    }

    [Fact]
    public void GetWeather_ShuffledQueryOrder_ReturnsSameValues()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());
        List<DateTimeOffset> times = new List<DateTimeOffset>();
        for (int minute = 0; minute < 180; minute += 7)
        {
            times.Add(NOON.AddMinutes(minute));
        }

        // Act
        Dictionary<DateTimeOffset, WeatherDto> forward = new Dictionary<DateTimeOffset, WeatherDto>();
        foreach (DateTimeOffset time in times)
        {
            forward[time] = model.getWeather(TestDataSets.LOCATION_ID, time);
        }

        times.Reverse();
        Dictionary<DateTimeOffset, WeatherDto> backward = new Dictionary<DateTimeOffset, WeatherDto>();
        foreach (DateTimeOffset time in times)
        {
            backward[time] = model.getWeather(TestDataSets.LOCATION_ID, time);
        }

        // Assert
        foreach (DateTimeOffset time in forward.Keys)
        {
            assertSameValues(forward[time], backward[time]);
        }
    }

    [Fact]
    public void FloorToMinute_SecondsDiffer_SameMinute()
    {
        // Arrange
        DateTimeOffset early = NOON.AddSeconds(1);
        DateTimeOffset late = NOON.AddSeconds(59).AddMilliseconds(900);

        // Act
        DateTimeOffset flooredEarly = HttpWeatherClient.floorToMinute(early);
        DateTimeOffset flooredLate = HttpWeatherClient.floorToMinute(late);

        // Assert
        Assert.Equal(NOON, flooredEarly);
        Assert.Equal(NOON, flooredLate);
    }

    [Fact]
    public async Task GetWeatherAsync_TwoClientsSameMinute_AskSameUrl()
    {
        // Arrange
        FakeWeatherHandler handlerOne = new FakeWeatherHandler();
        FakeWeatherHandler handlerTwo = new FakeWeatherHandler();
        handlerOne.Answer = new WeatherDto();
        handlerTwo.Answer = new WeatherDto();
        HttpWeatherClient clientOne = new HttpWeatherClient(new HttpClient(handlerOne), "http://localhost:5200");
        HttpWeatherClient clientTwo = new HttpWeatherClient(new HttpClient(handlerTwo), "http://localhost:5200/");

        // Act
        await clientOne.getWeatherAsync("salzburg", NOON.AddSeconds(3), CancellationToken.None);
        await clientTwo.getWeatherAsync("salzburg", NOON.AddSeconds(41), CancellationToken.None);

        // Assert
        Assert.Equal(handlerOne.RequestedUrls[0], handlerTwo.RequestedUrls[0]);
        Assert.EndsWith("time=2025-07-01T12%3A00%3A00Z", handlerOne.RequestedUrls[0]);
    }

    [Fact]
    public async Task GetWeatherAsync_ServiceDownAfterSuccess_ReturnsLastGoodValueStale()
    {
        // Arrange
        FakeWeatherHandler handler = new FakeWeatherHandler();
        handler.Answer = new WeatherDto { LocationId = "salzburg", TemperatureC = 21.5 };
        HttpWeatherClient client = new HttpWeatherClient(new HttpClient(handler), "http://localhost:5200");
        await client.getWeatherAsync("salzburg", NOON, CancellationToken.None);
        handler.IsDown = true;

        // Act
        WeatherResult result = await client.getWeatherAsync("salzburg", NOON.AddMinutes(5), CancellationToken.None);

        // Assert
        Assert.NotNull(result.Weather);
        Assert.Equal(21.5, result.Weather.TemperatureC);
        Assert.True(result.IsStale);
        Assert.Contains("unreachable", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWeatherAsync_ServiceDownNeverAnswered_ReturnsNoWeather()
    {
        // Arrange
        FakeWeatherHandler handler = new FakeWeatherHandler();
        handler.IsDown = true;
        HttpWeatherClient client = new HttpWeatherClient(new HttpClient(handler), "http://localhost:5200");

        // Act
        WeatherResult result = await client.getWeatherAsync("salzburg", NOON, CancellationToken.None);

        // Assert
        Assert.Null(result.Weather);
        Assert.False(result.IsStale);
        Assert.NotEqual(string.Empty, result.ErrorMessage);
    }

    private static void assertSameValues(WeatherDto expected, WeatherDto actual)
    {
        Assert.Equal(expected.TimeUtc, actual.TimeUtc);
        Assert.Equal(expected.SunElevationDeg, actual.SunElevationDeg);
        Assert.Equal(expected.SunAzimuthDeg, actual.SunAzimuthDeg);
        Assert.Equal(expected.CloudCoverPct, actual.CloudCoverPct);
        Assert.Equal(expected.TemperatureC, actual.TemperatureC);
        Assert.Equal(expected.PrecipitationMmPerH, actual.PrecipitationMmPerH);
        Assert.Equal(expected.WindSpeedMs, actual.WindSpeedMs);
        Assert.Equal(expected.WindDirectionDeg, actual.WindDirectionDeg);
        Assert.Equal(expected.GlobalIrradianceWm2, actual.GlobalIrradianceWm2);
    }
}
