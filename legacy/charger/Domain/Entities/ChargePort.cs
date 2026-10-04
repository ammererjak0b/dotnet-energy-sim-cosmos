using LB_01.Domain.Snapshots;

namespace LB_01.Domain.Entities;

public class ChargePort
{
    public int PortNumber { get; init; }

    public double MaximumPowerKw { get; init; }

    public ECar? ConnectedCar { get; private set; }

    public bool IsOccupied => ConnectedCar is not null;

    // internal so that TryConnect can only be called from within the same assembly (e.g., by DualPortCharger)
    internal bool TryConnect(ECar car)
    {
        if (IsOccupied || car.ConnectedPort is not null)
        {
            return false;
        }

        ConnectedCar = car;
        car.ConnectedPort = this;
        return true;
    }

    internal bool TryDisconnect()
    {
        ECar? connectedCar = ConnectedCar;

        if (connectedCar is null)
        {
            return false;
        }

        ConnectedCar = null;
        connectedCar.ConnectedPort = null;
        return true;
    }

    public ChargePortSnapshot CreateSnapshot(
        TimeSpan simulationTime,
        double currentPowerKw,
        double accumulatedEnergyKwh)
    {
        ECarSnapshot? carSnapshot = ConnectedCar is null
            ? null
            : new ECarSnapshot(
                ConnectedCar.Id,
                ConnectedCar.Name,
                ConnectedCar.Manufacturer,
                ConnectedCar.Model,
                ConnectedCar.BatteryCapacityKwh,
                ConnectedCar.StateOfChargePercent,
                ConnectedCar.MaximumChargingPowerKw);

        return new ChargePortSnapshot(
            simulationTime,
            PortNumber,
            MaximumPowerKw,
            currentPowerKw,
            accumulatedEnergyKwh,
            carSnapshot);
    }
}
