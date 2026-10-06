using LB_01.src.Application;
using LB_01.src.Domain.Snapshots;

namespace LB_01.Tests;

public class ConsoleDashboardTests
{
    private const int PORT_NUMBER = 1;
    private const double MAXIMUM_POWER_KW = 11;
    private const double CURRENT_POWER_KW = 11;
    private const double ACCUMULATED_ENERGY_KWH = 2.75;
    private const int CAR_ID = 1;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const double TOTAL_POWER_KW = 11;
    private const double TOTAL_ENERGY_KWH = 2.75;

    [Fact]
    public void RenderContainsSimulationAndChargingInformation()
    {
        ECarSnapshot car = new(
            CAR_ID,
            "demo-car",
            "Demo",
            "Model",
            BATTERY_CAPACITY_KWH,
            STATE_OF_CHARGE_PERCENT,
            MAXIMUM_CHARGING_POWER_KW);
        ChargePortSnapshot port = new(
            TimeSpan.FromMinutes(15),
            PORT_NUMBER,
            MAXIMUM_POWER_KW,
            CURRENT_POWER_KW,
            ACCUMULATED_ENERGY_KWH,
            car);
        SimulationSnapshot snapshot = new(
            TimeSpan.FromMinutes(15),
            [port],
            TOTAL_POWER_KW,
            TOTAL_ENERGY_KWH);

        string output = ConsoleDashboard.render(snapshot);

        Assert.Contains("Simulation time: 00:15:00", output);
        Assert.Contains("Total power: 11.00 kW", output);
        Assert.Contains("Accumulated energy: 2.75 kWh", output);
        Assert.Contains("Port 1", output);
        Assert.Contains("demo-car", output);
        Assert.Contains("SOC: 60.0%", output);
        Assert.Contains("[######----]", output);
    }
}
