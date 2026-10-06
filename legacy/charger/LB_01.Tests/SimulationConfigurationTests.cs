using LB_01.Application;

namespace LB_01.Tests;

public class SimulationConfigurationTests
{
    private const string DEFAULT_CHARGER_NAME = "Demo Charger";
    private const double DEFAULT_PORT_ONE_POWER_KW = 11;
    private const double DEFAULT_PORT_TWO_POWER_KW = 11;
    private const double DEFAULT_TOTAL_POWER_KW = 22;
    private const int DEFAULT_CAR_COUNT = 1;
    private const double CUSTOM_PORT_ONE_POWER_KW = 15;
    private const double CUSTOM_PORT_TWO_POWER_KW = 7;
    private const double CUSTOM_TOTAL_POWER_KW = 22;
    private const int CUSTOM_CAR_COUNT = 2;

    [Fact]
    public void DefaultConfigurationUsesDemoValues()
    {
        SimulationConfiguration configuration = SimulationConfiguration.createDefault();

        Assert.Equal(DEFAULT_CHARGER_NAME, configuration.chargerName);
        Assert.Equal(DEFAULT_PORT_ONE_POWER_KW, configuration.portOneMaximumPowerKw);
        Assert.Equal(DEFAULT_PORT_TWO_POWER_KW, configuration.portTwoMaximumPowerKw);
        Assert.Equal(DEFAULT_TOTAL_POWER_KW, configuration.maximumTotalPowerKw);
        Assert.Equal(DEFAULT_CAR_COUNT, configuration.cars.Count);
    }

    [Fact]
    public void ManualConfigurationStoresProvidedChargerValues()
    {
        SimulationConfiguration configuration = SimulationConfiguration.create(
            "Test Charger",
            CUSTOM_PORT_ONE_POWER_KW,
            CUSTOM_PORT_TWO_POWER_KW,
            CUSTOM_TOTAL_POWER_KW,
            CUSTOM_CAR_COUNT);

        Assert.Equal("Test Charger", configuration.chargerName);
        Assert.Equal(CUSTOM_PORT_ONE_POWER_KW, configuration.portOneMaximumPowerKw);
        Assert.Equal(CUSTOM_PORT_TWO_POWER_KW, configuration.portTwoMaximumPowerKw);
        Assert.Equal(CUSTOM_TOTAL_POWER_KW, configuration.maximumTotalPowerKw);
        Assert.Equal(CUSTOM_CAR_COUNT, configuration.cars.Count);
    }

    [Fact]
    public void ResetCommandRequestsApplicationRestart()
    {
        SimulationController controller = new();

        Assert.True(controller.processCommand('x'));
        Assert.True(controller.shouldRestart);
        Assert.False(controller.shouldQuit);
    }
}
