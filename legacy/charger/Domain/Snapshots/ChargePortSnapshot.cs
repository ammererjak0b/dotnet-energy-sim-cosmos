namespace LB_01.Domain.Snapshots;

public sealed record ChargePortSnapshot(
    TimeSpan SimulationTime,
    int PortNumber,
    double MaximumPowerKw,
    double CurrentPowerKw,
    double AccumulatedEnergyKwh,
    ECarSnapshot? Car);
