namespace LB_01.Domain.Snapshots;

public sealed record ChargePortSnapshot(
    TimeSpan simulationTime,
    int portNumber,
    double maximumPowerKw,
    double currentPowerKw,
    double accumulatedEnergyKwh,
    ECarSnapshot? car);
