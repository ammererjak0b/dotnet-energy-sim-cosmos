using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Simulation;
using LB_01.src.Domain.Snapshots;

namespace LB_01.Tests;

public class ChargingSimulationTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int CAR_ID = 1;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double INITIAL_STATE_OF_CHARGE_PERCENT = 50;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const int SIMULATION_HOURS = 1;
    private const double EXPECTED_POWER_KW = 11;
    private const double EXPECTED_ENERGY_KWH = 11;
    private const double EXPECTED_STATE_OF_CHARGE_PERCENT = 68.3333333333;
    private const double STATE_OF_CHARGE_AT_TAPER_START_PERCENT = 80;
    private const double STATE_OF_CHARGE_WITH_REDUCED_POWER_PERCENT = 90;
    private const double EXPECTED_REDUCED_POWER_KW = 5.5;
    private const double START_STATE_OF_CHARGE_PERCENT = 70;
    private const double EXPECTED_FULL_CHARGE_ENERGY_KWH = 18;
    private const double FULL_STATE_OF_CHARGE_PERCENT = 100;
    private const int SIMULATION_STEP_MINUTES = 15;
    private const int MAXIMUM_SIMULATION_STEPS = 200;

    [Fact]
    public void ChargingForOneHourUpdatesEnergyPowerAndStateOfCharge()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new()
        {
            id = CAR_ID,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = INITIAL_STATE_OF_CHARGE_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
        charger.tryConnect(charger.port1, car);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromHours(SIMULATION_HOURS));
        ChargePortSnapshot portSnapshot = snapshot.ports[0];

        Assert.Equal(EXPECTED_POWER_KW, portSnapshot.currentPowerKw);
        Assert.Equal(EXPECTED_ENERGY_KWH, portSnapshot.accumulatedEnergyKwh);
        Assert.Equal(EXPECTED_STATE_OF_CHARGE_PERCENT, car.stateOfChargePercent, 2);
        Assert.NotNull(portSnapshot.car);
        Assert.Equal(EXPECTED_STATE_OF_CHARGE_PERCENT, portSnapshot.car.stateOfChargePercent, 2);
    }

    [Fact]
    public void ChargingAt80PercentUsesMaximumPower()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new()
        {
            id = CAR_ID,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = STATE_OF_CHARGE_AT_TAPER_START_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
        charger.tryConnect(charger.port1, car);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(PORT_MAXIMUM_POWER_KW, snapshot.ports[0].currentPowerKw);
    }

    [Fact]
    public void ChargingAt90PercentUsesReducedPower()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new()
        {
            id = CAR_ID,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = STATE_OF_CHARGE_WITH_REDUCED_POWER_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
        charger.tryConnect(charger.port1, car);
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(EXPECTED_REDUCED_POWER_KW, snapshot.ports[0].currentPowerKw, 2);
    }

    [Fact]
    public void ChargingFrom70To100PercentCompletesWithReducedPower()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new()
        {
            id = CAR_ID,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = START_STATE_OF_CHARGE_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
        charger.tryConnect(charger.port1, car);
        Simulation simulation = new(charger);
        List<double> chargingPowersKw = [];
        int simulationSteps = 0;
        SimulationSnapshot snapshot = simulation.advance(TimeSpan.Zero);

        while (car.stateOfChargePercent < FULL_STATE_OF_CHARGE_PERCENT
            && simulationSteps < MAXIMUM_SIMULATION_STEPS)
        {
            snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));
            chargingPowersKw.Add(snapshot.ports[0].currentPowerKw);
            simulationSteps++;
        }

        ChargePortSnapshot portSnapshot = snapshot.ports[0];

        Assert.True(simulationSteps < MAXIMUM_SIMULATION_STEPS);
        Assert.Equal(FULL_STATE_OF_CHARGE_PERCENT, car.stateOfChargePercent, 2);
        Assert.Equal(EXPECTED_FULL_CHARGE_ENERGY_KWH, portSnapshot.accumulatedEnergyKwh, 2);
        Assert.Equal(0, portSnapshot.currentPowerKw, 2);
        Assert.Contains(chargingPowersKw, powerKw => powerKw > 0 && powerKw < PORT_MAXIMUM_POWER_KW);
    }
}
