using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Simulation;
using LB_01.src.Domain.Snapshots;

namespace LB_01.Tests;

public class ParallelChargingSimulationTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_ONE_MAXIMUM_POWER_KW = 11;
    private const double PORT_TWO_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 11;
    private const int FIRST_CAR_ID = 1;
    private const int SECOND_CAR_ID = 2;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double INITIAL_STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 15;
    private const int SIMULATION_STEP_MINUTES = 15;
    private const double EXPECTED_SHARED_POWER_KW = 5.5;
    private const double EXPECTED_SHARED_ENERGY_KWH = 1.375;
    private const double REDUCED_STATE_OF_CHARGE_PERCENT = 90;
    private const double EXPECTED_TAPER_POWER_KW = 5.5;
    private const double EXPECTED_TOTAL_POWER_KW = 11;
    private const double EXPECTED_PORT_ONE_POWER_KW = 5.5;
    private const double EXPECTED_PORT_TWO_POWER_KW = 5.5;

    [Fact]
    public void TwoCarsShareLimitedStationPowerFairly()
    {
        DualPortCharger charger = createCharger(
            PORT_ONE_MAXIMUM_POWER_KW,
            PORT_TWO_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        connectCars(charger, INITIAL_STATE_OF_CHARGE_PERCENT, INITIAL_STATE_OF_CHARGE_PERCENT);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(EXPECTED_SHARED_POWER_KW, snapshot.ports[0].currentPowerKw, 2);
        Assert.Equal(EXPECTED_SHARED_POWER_KW, snapshot.ports[1].currentPowerKw, 2);
        Assert.Equal(EXPECTED_TOTAL_POWER_KW, snapshot.totalPowerKw, 2);
        Assert.Equal(EXPECTED_SHARED_ENERGY_KWH, snapshot.ports[0].accumulatedEnergyKwh, 2);
        Assert.Equal(EXPECTED_SHARED_ENERGY_KWH, snapshot.ports[1].accumulatedEnergyKwh, 2);
    }

    [Fact]
    public void DifferentPortLimitsAreRespectedAndRemainingPowerIsDistributed()
    {
        DualPortCharger charger = createCharger(
            portOneMaximumPowerKw: 15,
            portTwoMaximumPowerKw: 7,
            maximumTotalPowerKw: 22);
        connectCars(charger, INITIAL_STATE_OF_CHARGE_PERCENT, INITIAL_STATE_OF_CHARGE_PERCENT);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(15, snapshot.ports[0].currentPowerKw, 2);
        Assert.Equal(7, snapshot.ports[1].currentPowerKw, 2);
        Assert.Equal(22, snapshot.totalPowerKw, 2);
    }

    [Fact]
    public void TaperedCarDoesNotConsumePowerTheOtherCarCanUse()
    {
        DualPortCharger charger = createCharger(
            PORT_ONE_MAXIMUM_POWER_KW,
            PORT_TWO_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        connectCars(charger, REDUCED_STATE_OF_CHARGE_PERCENT, INITIAL_STATE_OF_CHARGE_PERCENT);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(EXPECTED_TAPER_POWER_KW, snapshot.ports[0].currentPowerKw, 2);
        Assert.Equal(EXPECTED_PORT_TWO_POWER_KW, snapshot.ports[1].currentPowerKw, 2);
        Assert.Equal(EXPECTED_TOTAL_POWER_KW, snapshot.totalPowerKw, 2);
    }

    private static DualPortCharger createCharger(
        double portOneMaximumPowerKw,
        double portTwoMaximumPowerKw,
        double maximumTotalPowerKw)
    {
        return new DualPortCharger(
            CHARGER_ID,
            CHARGER_NAME,
            portOneMaximumPowerKw,
            portTwoMaximumPowerKw,
            maximumTotalPowerKw);
    }

    private static void connectCars(
        DualPortCharger charger,
        double firstStateOfChargePercent,
        double secondStateOfChargePercent)
    {
        ECar firstCar = createCar(FIRST_CAR_ID, firstStateOfChargePercent);
        ECar secondCar = createCar(SECOND_CAR_ID, secondStateOfChargePercent);
        charger.tryConnect(charger.port1, firstCar);
        charger.tryConnect(charger.port2, secondCar);
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
