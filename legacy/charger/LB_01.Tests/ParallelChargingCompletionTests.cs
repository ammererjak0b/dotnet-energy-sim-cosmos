using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class ParallelChargingCompletionTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 11;
    private const int FIRST_CAR_ID = 1;
    private const int SECOND_CAR_ID = 2;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double FIRST_INITIAL_STATE_OF_CHARGE_PERCENT = 99.6;
    private const double SECOND_INITIAL_STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const int SIMULATION_STEP_MINUTES = 15;
    private const double EXPECTED_FIRST_PORT_POWER_KW = 0;
    private const double EXPECTED_SECOND_PORT_POWER_KW = 11;
    private const double FULL_STATE_OF_CHARGE_PERCENT = 100;

    [Fact]
    public void RemainingPowerIsDistributedAfterFirstCarReachesFullCharge()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar firstCar = createCar(FIRST_CAR_ID, FIRST_INITIAL_STATE_OF_CHARGE_PERCENT);
        ECar secondCar = createCar(SECOND_CAR_ID, SECOND_INITIAL_STATE_OF_CHARGE_PERCENT);
        charger.tryConnect(charger.port1, firstCar);
        charger.tryConnect(charger.port2, secondCar);
        Simulation simulation = new(charger);

        simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));
        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(FULL_STATE_OF_CHARGE_PERCENT, firstCar.stateOfChargePercent, 2);
        Assert.Equal(EXPECTED_FIRST_PORT_POWER_KW, snapshot.ports[0].currentPowerKw, 2);
        Assert.Equal(EXPECTED_SECOND_PORT_POWER_KW, snapshot.ports[1].currentPowerKw, 2);
        Assert.Equal(MAXIMUM_TOTAL_POWER_KW, snapshot.totalPowerKw, 2);
    }

    private static ECar createCar(int id, double stateOfChargePercent)
    {
        return new ECar
        {
            id = id,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = stateOfChargePercent,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
    }
}
