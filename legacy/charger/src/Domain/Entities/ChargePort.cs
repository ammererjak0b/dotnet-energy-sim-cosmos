using LB_01.src.Domain.Snapshots;

namespace LB_01.src.Domain.Entities;

public class ChargePort
{
    public int portNumber { get; init; }

    public double maximumPowerKw { get; init; }

    public double currentPowerKw { get; internal set; }

    public double accumulatedEnergyKwh { get; internal set; }

    public string? fault { get; private set; }

    public ECar? connectedCar { get; private set; }

    public bool isOccupied => connectedCar is not null;

    public bool hasFault => fault is not null;

    public void setFault(string faultDescription)
    {
        fault = faultDescription;
        currentPowerKw = 0;
    }

    public void clearFault()
    {
        fault = null;
    }

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
        return createSnapshot(
            simulationTime,
            currentPowerKw,
            accumulatedEnergyKwh,
            getStatus(),
            fault);
    }

    public ChargePortSnapshot createSnapshot(
        TimeSpan simulationTime,
        double currentPowerKw,
        double accumulatedEnergyKwh,
        ChargePortStatus status,
        string? fault)
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
            status,
            fault,
            carSnapshot);
    }

    public ChargePortStatus getStatus()
    {
        if (hasFault)
        {
            return ChargePortStatus.Fault;
        }

        if (currentPowerKw > 0)
        {
            return ChargePortStatus.Charging;
        }

        return ChargePortStatus.Available;
    }
}
