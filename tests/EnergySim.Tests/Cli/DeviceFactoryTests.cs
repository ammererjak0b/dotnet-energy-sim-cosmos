using EnergySim.Cli;
using EnergySim.Terminal;
using EnergySim.Core.Devices;
using EnergySim.Battery;
using EnergySim.Charger;
using EnergySim.Pv;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Tests.Cli;

public class DeviceFactoryTests
{
    private static IConfiguration createConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static CliSettings createSettings(string device)
    {
        return CliSettings.load(createConfiguration(new Dictionary<string, string?> { { "device", device } }));
    }

    [Theory]
    [InlineData("pv", DeviceType.Pv)]
    [InlineData("battery", DeviceType.Battery)]
    [InlineData("charger", DeviceType.Charger)]
    public void Create_NoSection_UsesClassDefaults(string device, DeviceType expectedType)
    {
        // Act
        ISimulatedDevice created = DeviceFactory.create(createSettings(device), createConfiguration(new Dictionary<string, string?>()));

        // Assert
        Assert.Equal(expectedType, created.Type);
        Assert.Equal(device + "-1", created.DeviceId);
    }

    [Fact]
    public void Create_PvSection_OverridesDefaults()
    {
        // Arrange
        IConfiguration configuration = createConfiguration(new Dictionary<string, string?> { { "Pv:PeakPowerKw", "10" } });

        // Act
        PvOptions options = DeviceFactory.bindPvOptions(configuration);

        // Assert
        Assert.Equal(10.0, options.PeakPowerKw);
        Assert.Equal(0.8, options.PerformanceRatio); // default stays
    }

    [Fact]
    public void BindChargerOptions_ScheduleInConfig_ReplacesDefaultSchedule()
    {
        // Arrange
        IConfiguration configuration = createConfiguration(new Dictionary<string, string?>
        {
            { "Charger:Schedule:0:Name", "Night Car" },
            { "Charger:Schedule:0:Port", "2" },
            { "Charger:Schedule:0:ArriveHourUtc", "22" },
            { "Charger:Schedule:0:DepartHourUtc", "5" }
        });

        // Act
        ChargerOptions options = DeviceFactory.bindChargerOptions(configuration);

        // Assert
        Assert.Single(options.Schedule);
        Assert.Equal("Night Car", options.Schedule[0].Name);
        Assert.Equal(2, options.Schedule[0].Port);
    }

    [Fact]
    public void BindChargerOptions_NoSchedule_KeepsDefaultSchedule()
    {
        // Act
        ChargerOptions options = DeviceFactory.bindChargerOptions(createConfiguration(new Dictionary<string, string?> { { "Charger:TotalMaxPowerKw", "30" } }));

        // Assert
        Assert.Equal(30.0, options.TotalMaxPowerKw);
        Assert.True(options.Schedule.Count >= 2);
    }

    [Fact]
    public void Create_InvalidOptions_ThrowsUsageError()
    {
        // Arrange
        IConfiguration configuration = createConfiguration(new Dictionary<string, string?> { { "Battery:CapacityKWh", "-5" } });

        // Act + Assert
        CliUsageException exception = Assert.Throws<CliUsageException>(delegate { DeviceFactory.create(createSettings("battery"), configuration); });
        Assert.Contains("Battery", exception.Message);
    }
}
