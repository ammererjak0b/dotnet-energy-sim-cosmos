using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

public class LocalWeatherClientTests
{
    private static readonly DateTimeOffset NOON = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetWeatherAsync_KnownLocation_ReturnsModelValueNotStale()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());
        LocalWeatherClient client = new LocalWeatherClient(model);

        // Act
        WeatherResult result = await client.getWeatherAsync("salzburg", NOON, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Weather);
        Assert.Equal(model.getWeather("salzburg", NOON).TemperatureC, result.Weather.TemperatureC);
        Assert.False(result.IsStale);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public async Task GetWeatherAsync_UnknownLocation_ReturnsNoWeatherWithMessage()
    {
        // Arrange
        LocalWeatherClient client = new LocalWeatherClient(TestDataSets.createModel(TestDataSets.createRamp()));

        // Act
        WeatherResult result = await client.getWeatherAsync("mars", NOON, CancellationToken.None);

        // Assert
        Assert.Null(result.Weather);
        Assert.Contains("mars", result.ErrorMessage);
    }

    [Fact]
    public async Task GetWeatherAsync_CancelledToken_Throws()
    {
        // Arrange
        LocalWeatherClient client = new LocalWeatherClient(TestDataSets.createModel(TestDataSets.createRamp()));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(delegate { return client.getWeatherAsync("salzburg", NOON, cancellation.Token); });
    }
}
