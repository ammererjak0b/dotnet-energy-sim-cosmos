using LB_01.src.Domain.Entities;

namespace LB_01.src.Domain.Snapshots;

public sealed record ChargePortSnapshot(
    TimeSpan simulationTime,
    int portNumber,
    double maximumPowerKw,
    double currentPowerKw,
    double accumulatedEnergyKwh,
    ChargePortStatus status,
    string? fault,
    ECarSnapshot? car)
{
    public ChargePortSnapshot(
        TimeSpan simulationTime,
        int portNumber,
        double maximumPowerKw,
        double currentPowerKw,
        double accumulatedEnergyKwh,
        ECarSnapshot? car)
        : this(
            simulationTime,
            portNumber,
            maximumPowerKw,
            currentPowerKw,
            accumulatedEnergyKwh,
            currentPowerKw > 0 ? ChargePortStatus.Charging : ChargePortStatus.Available,
            null,
            car)
    {
    }
}
