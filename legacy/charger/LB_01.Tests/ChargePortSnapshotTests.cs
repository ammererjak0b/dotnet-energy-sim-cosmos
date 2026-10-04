using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class ChargePortSnapshotTests
{
    private const int ChargerId = 1;
    private const string ChargerName = "charger-1";
    private const double PortMaximumPowerKw = 11;
    private const double MaximumTotalPowerKw = 22;
    private const int CarId = 1;
    private const string CarName = "car-1";
    private const string Manufacturer = "Example Motors";
    private const string Model = "Example E";
    private const double BatteryCapacityKwh = 60;
    private const double StateOfChargePercent = 40;
    private const double MaximumChargingPowerKw = 11;
    private const double CurrentPowerKw = 9.5;
    private const double AccumulatedEnergyKwh = 2.375;
    private const int SimulationMinutes = 15;

    [Fact]
    public void CreatesSnapshotWithPortAndConnectedCarData()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar car = new()
        {
            Id = CarId,
            Name = CarName,
            Manufacturer = Manufacturer,
            Model = Model,
            BatteryCapacityKwh = BatteryCapacityKwh,
            StateOfChargePercent = StateOfChargePercent,
            MaximumChargingPowerKw = MaximumChargingPowerKw
        };
        charger.TryConnect(charger.Port1, car);
        TimeSpan simulationTime = TimeSpan.FromMinutes(SimulationMinutes);

        ChargePortSnapshot snapshot = charger.Port1.CreateSnapshot(
            simulationTime,
            CurrentPowerKw,
            AccumulatedEnergyKwh);

        Assert.Equal(simulationTime, snapshot.SimulationTime);
        Assert.Equal(charger.Port1.PortNumber, snapshot.PortNumber);
        Assert.Equal(PortMaximumPowerKw, snapshot.MaximumPowerKw);
        Assert.Equal(CurrentPowerKw, snapshot.CurrentPowerKw);
        Assert.Equal(AccumulatedEnergyKwh, snapshot.AccumulatedEnergyKwh);
        Assert.NotNull(snapshot.Car);
        Assert.Equal(CarId, snapshot.Car.Id);
        Assert.Equal(CarName, snapshot.Car.Name);
        Assert.Equal(Manufacturer, snapshot.Car.Manufacturer);
        Assert.Equal(Model, snapshot.Car.Model);
        Assert.Equal(BatteryCapacityKwh, snapshot.Car.BatteryCapacityKwh);
        Assert.Equal(StateOfChargePercent, snapshot.Car.StateOfChargePercent);
        Assert.Equal(MaximumChargingPowerKw, snapshot.Car.MaximumChargingPowerKw);
    }
}
