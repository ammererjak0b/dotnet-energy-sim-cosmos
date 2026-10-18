using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

/// <summary>Integration: real generated Open-Meteo files in src/EnergySim.Weather/data.</summary>
public class RealDataTests
{
    private static string? findDataDirectory()
    {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            string candidate = Path.Combine(current.FullName, "src", "EnergySim.Weather", "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = current.Parent;
        }
        return null;
    }

    [Fact]
    public async Task RealFiles_LoadAndProducePlausibleWeather()
    {
        // Arrange
        string? directory = findDataDirectory();
        Assert.NotNull(directory); // data folder must exist

        // Act
        IReadOnlyDictionary<string, WeatherDataSet> dataSets = await WeatherDataLoader.loadDirectoryAsync(directory, CancellationToken.None);
        WeatherModel model = new WeatherModel(dataSets);

        // Assert
        foreach (string id in LocationCatalog.ids())
        {
            WeatherDto summer = model.getWeather(id, new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero));
            WeatherDto winter = model.getWeather(id, new DateTimeOffset(2026, 1, 15, 11, 0, 0, TimeSpan.Zero));
            Assert.InRange(summer.TemperatureC, -5, 45);
            Assert.InRange(summer.CloudCoverPct, 0, 100);
            Assert.InRange(summer.GlobalIrradianceWm2, 0, 1200);
            Assert.InRange(summer.WindSpeedMs, 0, 40);
            Assert.True(summer.TemperatureC > winter.TemperatureC);
            Assert.True(summer.SunElevationDeg > winter.SunElevationDeg);
        }
    }
}
