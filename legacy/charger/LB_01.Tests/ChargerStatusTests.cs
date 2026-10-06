using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class ChargerStatusTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int CAR_ID = 1;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double INITIAL_STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const int SIMULATION_STEP_MINUTES = 15;

    [Fact]
    public void TurnedOffChargerAdvancesTimeWithoutCharging()
    {
        DualPortCharger charger = createCharger();
        ECar car = createCar(CAR_ID);
        charger.tryConnect(charger.port1, car);
        charger.turnOff();
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES), snapshot.simulationTime);
        Assert.Equal(0, snapshot.totalPowerKw);
        Assert.Equal(ChargerStatus.Off, snapshot.chargerStatus);
        Assert.Equal(ChargePortStatus.Off, snapshot.ports[0].status);
        Assert.Equal(INITIAL_STATE_OF_CHARGE_PERCENT, car.stateOfChargePercent);
    }

    [Fact]
    public void ChargerFaultTurnsBothPortsOff()
    {
        DualPortCharger charger = createCharger();
        charger.setFault("charger fault");
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(ChargerStatus.Fault, snapshot.chargerStatus);
        Assert.Equal("charger fault", snapshot.chargerFault);
        Assert.Equal(ChargePortStatus.Off, snapshot.ports[0].status);
        Assert.Equal(ChargePortStatus.Off, snapshot.ports[1].status);
    }

    [Fact]
    public void PortFaultStopsOnlyAffectedPort()
    {
        DualPortCharger charger = createCharger();
        ECar firstCar = createCar(CAR_ID);
        ECar secondCar = createCar(2);
        charger.tryConnect(charger.port1, firstCar);
        charger.tryConnect(charger.port2, secondCar);
        charger.port1.setFault("port fault");
        Simulation simulation = new(charger);

        SimulationSnapshot snapshot = simulation.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(ChargerStatus.On, snapshot.chargerStatus);
        Assert.Equal(ChargePortStatus.Fault, snapshot.ports[0].status);
        Assert.Equal(ChargePortStatus.Charging, snapshot.ports[1].status);
        Assert.Equal(0, snapshot.ports[0].currentPowerKw);
        Assert.True(snapshot.ports[1].currentPowerKw > 0);
    }

    private static DualPortCharger createCharger()
    {
        return new DualPortCharger(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
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
