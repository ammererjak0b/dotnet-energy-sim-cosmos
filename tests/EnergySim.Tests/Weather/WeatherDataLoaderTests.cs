using System.Globalization;
using System.Text;
using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

public class WeatherDataLoaderTests : IDisposable
{
    private readonly string _directory;

    public WeatherDataLoaderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "energysim-weather-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    private static string array(int rows, double value, bool asNull)
    {
        StringBuilder builder = new StringBuilder("[");
        for (int row = 0; row < rows; row++)
        {
            if (row > 0)
            {
                builder.Append(',');
            }
            if (asNull && row == 3)
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(value.ToString(CultureInfo.InvariantCulture));
            }
        }
        builder.Append(']');
        return builder.ToString();
    }

    private string writeFile(string name, int rows, string windUnit, bool withNull)
    {
        string json = "{\"hourly_units\":{\"wind_speed_10m\":\"" + windUnit + "\"},\"hourly\":{"
            + "\"time\":" + array(rows, 0, false).Replace("0", "\"t\"") + ","
            + "\"temperature_2m\":" + array(rows, 10, withNull) + ","
            + "\"cloud_cover\":" + array(rows, 50, false) + ","
            + "\"shortwave_radiation\":" + array(rows, 100, false) + ","
            + "\"precipitation\":" + array(rows, 1, false) + ","
            + "\"wind_speed_10m\":" + array(rows, 36, false) + ","
            + "\"wind_direction_10m\":" + array(rows, 90, false) + "}}";
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public async Task LoadFileAsync_ValidFile_ReadsAllHours()
    {
        // Arrange
        string path = writeFile("a.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", false);

        // Act
        WeatherDataSet dataSet = await WeatherDataLoader.loadFileAsync(path, CancellationToken.None);

        // Assert
        Assert.Equal(WeatherDataSet.HOURS_PER_YEAR, dataSet.TemperatureC.Length);
        Assert.Equal(10, dataSet.TemperatureC[100]);
        Assert.Equal(36, dataSet.WindSpeedMs[100]);
    }

    [Fact]
    public async Task LoadFileAsync_KmhUnit_ConvertsToMetersPerSecond()
    {
        // Arrange
        string path = writeFile("kmh.json", WeatherDataSet.HOURS_PER_YEAR, "km/h", false);

        // Act
        WeatherDataSet dataSet = await WeatherDataLoader.loadFileAsync(path, CancellationToken.None);

        // Assert
        Assert.Equal(10, dataSet.WindSpeedMs[0], 6); // 36 km/h = 10 m/s
    }

    [Fact]
    public async Task LoadFileAsync_WrongRowCount_Throws()
    {
        // Arrange
        string path = writeFile("short.json", 100, "m/s", false);

        // Act
        Task act() => WeatherDataLoader.loadFileAsync(path, CancellationToken.None);

        // Assert
        WeatherDataException exception = await Assert.ThrowsAsync<WeatherDataException>(act);
        Assert.Contains("8760", exception.Message);
    }

    [Fact]
    public async Task LoadFileAsync_NullValue_Throws()
    {
        // Arrange
        string path = writeFile("null.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", true);

        // Act
        Task act() => WeatherDataLoader.loadFileAsync(path, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<WeatherDataException>(act);
    }

    [Fact]
    public async Task LoadFileAsync_MissingFile_Throws()
    {
        // Act
        Task act() => WeatherDataLoader.loadFileAsync(Path.Combine(_directory, "nope.json"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<WeatherDataException>(act);
    }

    [Fact]
    public async Task LoadDirectoryAsync_AllLocationFiles_ReturnsModelData()
    {
        // Arrange
        writeFile("salzburg-2025.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", false);
        writeFile("wien-2025.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", false);
        writeFile("innsbruck-2025.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", false);

        // Act
        IReadOnlyDictionary<string, WeatherDataSet> dataSets = await WeatherDataLoader.loadDirectoryAsync(_directory, CancellationToken.None);

        // Assert
        Assert.Equal(3, dataSets.Count);
        Assert.True(dataSets.ContainsKey("wien"));
    }

    [Fact]
    public async Task LoadDirectoryAsync_MissingLocationFile_Throws()
    {
        // Arrange
        writeFile("salzburg-2025.json", WeatherDataSet.HOURS_PER_YEAR, "m/s", false);

        // Act
        Task act() => WeatherDataLoader.loadDirectoryAsync(_directory, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<WeatherDataException>(act);
    }
}
