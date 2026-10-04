using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class SimulationAggregateTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int FIRST_CAR_ID = 1;
    private const int SECOND_CAR_ID = 2;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double INITIAL_STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const int SIMULATION_STEP_MINUTES = 15;
    private const double EXPECTED_TOTAL_POWER_KW = 22;
    private const double EXPECTED_TOTAL_ENERGY_KWH = 5.5;

    [Fact]
    public void SnapshotContainsTotalPowerAndAccumulatedEnergy()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar firstCar = createCar(FIRST_CAR_ID);
        ECar secondCar = createCar(SECOND_CAR_ID);
        charger.tryConnect(charger.port1, firstCar);
        charger.tryConnect(charger.port2, secondCar);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(EXPECTED_TOTAL_POWER_KW, snapshot.totalPowerKw);
        Assert.Equal(EXPECTED_TOTAL_ENERGY_KWH, snapshot.accumulatedEnergyKwh);
    }

    private static ECar createCar(int id)
    {
        return new ECar
        {
            id = id,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = INITIAL_STATE_OF_CHARGE_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
    }
}
