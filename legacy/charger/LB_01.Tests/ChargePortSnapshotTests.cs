using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class ChargePortSnapshotTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int CAR_ID = 1;
    private const string CAR_NAME = "car-1";
    private const string MANUFACTURER = "Example Motors";
    private const string MODEL = "Example E";
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double STATE_OF_CHARGE_PERCENT = 40;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const double CURRENT_POWER_KW = 9.5;
    private const double ACCUMULATED_ENERGY_KWH = 2.375;
    private const int SIMULATION_MINUTES = 15;

    [Fact]
    public void CreatesSnapshotWithPortAndConnectedCarData()
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
            name = CAR_NAME,
            manufacturer = MANUFACTURER,
            model = MODEL,
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = STATE_OF_CHARGE_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
        charger.tryConnect(charger.port1, car);
        TimeSpan simulationTime = TimeSpan.FromMinutes(SIMULATION_MINUTES);

        ChargePortSnapshot snapshot = charger.port1.createSnapshot(
            simulationTime,
            CURRENT_POWER_KW,
            ACCUMULATED_ENERGY_KWH);

        Assert.Equal(simulationTime, snapshot.simulationTime);
        Assert.Equal(charger.port1.portNumber, snapshot.portNumber);
        Assert.Equal(PORT_MAXIMUM_POWER_KW, snapshot.maximumPowerKw);
        Assert.Equal(CURRENT_POWER_KW, snapshot.currentPowerKw);
        Assert.Equal(ACCUMULATED_ENERGY_KWH, snapshot.accumulatedEnergyKwh);
        Assert.NotNull(snapshot.car);
        Assert.Equal(CAR_ID, snapshot.car.id);
        Assert.Equal(CAR_NAME, snapshot.car.name);
        Assert.Equal(MANUFACTURER, snapshot.car.manufacturer);
        Assert.Equal(MODEL, snapshot.car.model);
        Assert.Equal(BATTERY_CAPACITY_KWH, snapshot.car.batteryCapacityKwh);
        Assert.Equal(STATE_OF_CHARGE_PERCENT, snapshot.car.stateOfChargePercent);
        Assert.Equal(MAXIMUM_CHARGING_POWER_KW, snapshot.car.maximumChargingPowerKw);
    }
}
