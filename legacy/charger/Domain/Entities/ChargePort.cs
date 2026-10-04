using LB_01.Domain.Snapshots;

namespace LB_01.Domain.Entities;

public class ChargePort
{
    public int portNumber { get; init; }

    public double maximumPowerKw { get; init; }

    public ECar? connectedCar { get; private set; }

    public bool isOccupied => connectedCar is not null;

    // internal so that TryConnect can only be called from within the same assembly (e.g., by DualPortCharger)
    internal bool tryConnect(ECar car)
    {
        if (isOccupied || car.connectedPort is not null)
        {
            return false;
        }

        connectedCar = car;
        car.connectedPort = this;
        return true;
    }

    internal bool tryDisconnect()
    {
        ECar? connectedCarToDisconnect = connectedCar;

        if (connectedCarToDisconnect is null)
        {
            return false;
        }

        connectedCar = null;
        connectedCarToDisconnect.connectedPort = null;
        return true;
    }

    public ChargePortSnapshot createSnapshot(
        TimeSpan simulationTime,
        double currentPowerKw,
        double accumulatedEnergyKwh)
    {
        ECarSnapshot? carSnapshot = connectedCar is null
            ? null
            : new ECarSnapshot(
                connectedCar.id,
                connectedCar.name,
                connectedCar.manufacturer,
                connectedCar.model,
                connectedCar.batteryCapacityKwh,
                connectedCar.stateOfChargePercent,
                connectedCar.maximumChargingPowerKw);

        return new ChargePortSnapshot(
            simulationTime,
            portNumber,
            maximumPowerKw,
            currentPowerKw,
            accumulatedEnergyKwh,
            carSnapshot);
    }
}
