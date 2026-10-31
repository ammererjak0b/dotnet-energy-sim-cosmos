using EnergySim.Cli;
using EnergySim.Terminal;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Tests.Cli;

public class CliSettingsTests
{
    private static IConfiguration createConfiguration(params string[] args)
    {
        return new ConfigurationBuilder().AddCommandLine(args).Build();
    }

    [Fact]
    public void Load_NoArguments_UsesDefaults()
    {
        // Act
        CliSettings settings = CliSettings.load(createConfiguration());

        // Assert
        Assert.Equal(string.Empty, settings.Device);
        Assert.Equal(new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero), settings.Start);
        Assert.Equal(60.0, settings.Speed);
        Assert.Equal("salzburg", settings.Location);
        Assert.Equal(string.Empty, settings.CsvPath);
    }

    [Fact]
    public void Load_AllArguments_AreParsed()
    {
        // Act
        CliSettings settings = CliSettings.load(createConfiguration("--device", "Battery", "--id", "bat-7", "--start", "2026-01-02T03:04:05Z", "--speed", "3600", "--location", "wien", "--csv", "/tmp/x.csv"));

        // Assert
        Assert.Equal("battery", settings.Device);
        Assert.Equal("bat-7", settings.Id);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), settings.Start);
        Assert.Equal(3600.0, settings.Speed);
        Assert.Equal("wien", settings.Location);
        Assert.Equal("/tmp/x.csv", settings.CsvPath);
    }

    [Fact]
    public void Load_NoId_UsesDeviceNameWithNumber()
    {
        // Act
        CliSettings settings = CliSettings.load(createConfiguration("--device", "charger"));

        // Assert
        Assert.Equal("charger-1", settings.Id);
    }

    [Fact]
    public void Load_SharedClock_UsesDevicePortDefaultsAndConfiguredMasterPort()
    {
        // Arrange

        // Act
        CliSettings settings = CliSettings.load(createConfiguration("--device", "battery", "--clock", "shared", "--time-port", "1550"));

        // Assert
        Assert.Equal("shared", settings.ClockMode);
        Assert.Equal(1602, settings.ClockPort);
        Assert.Equal(1550, settings.TimePort);
        Assert.Equal("127.0.0.1", settings.TimeHost);
    }

    [Fact]
    public void Load_SharedClockCustomPort_UsesConfiguredDevicePort()
    {
        // Arrange

        // Act
        CliSettings settings = CliSettings.load(createConfiguration("--device", "pv", "--clock", "shared", "--clock-port", "1701"));

        // Assert
        Assert.Equal(1701, settings.ClockPort);
    }

    [Fact]
    public void Load_StartWithoutZone_IsTreatedAsUtc()
    {
        // Act
        CliSettings settings = CliSettings.load(createConfiguration("--start", "2026-06-21T11:00:00"));

        // Assert
        Assert.Equal(new DateTimeOffset(2026, 6, 21, 11, 0, 0, TimeSpan.Zero), settings.Start);
    }

    [Theory]
    [InlineData("--device", "unknown")]
    [InlineData("--speed", "fast")]
    [InlineData("--speed", "0")]
    [InlineData("--speed", "4000")]
    [InlineData("--start", "yesterday")]
    [InlineData("--location", "mars")]
    [InlineData("--clock", "network")]
    [InlineData("--clock-port", "80")]
    [InlineData("--time-host", "192.168.1.4")]
    public void Load_InvalidValue_ThrowsUsageErrorNamingTheOption(string option, string value)
    {
        // Act
        CliUsageException exception = Assert.Throws<CliUsageException>(delegate { CliSettings.load(createConfiguration(option, value)); });

        // Assert
        Assert.Contains(option.Substring(2), exception.Message);
    }

    /// <summary>Accepts wind in the same selection list used by the interactive picker.</summary>
    [Fact]
    public void Load_WithWindDevice_UsesWindDefaultId()
    {
        // Arrange
        IConfiguration configuration = createConfiguration("--device", "Wind");

        // Act
        CliSettings settings = CliSettings.load(configuration);

        // Assert
        Assert.Equal("wind", settings.Device);
        Assert.Equal("wind-1", settings.Id);
        Assert.Contains("wind", CliSettings.DEVICE_NAMES);
    }
}
