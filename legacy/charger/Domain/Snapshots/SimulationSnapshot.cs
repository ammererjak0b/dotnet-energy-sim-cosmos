namespace LB_01.Domain.Snapshots;

public sealed record SimulationSnapshot(
    TimeSpan simulationTime,
    IReadOnlyList<ChargePortSnapshot> ports,
    double totalPowerKw,
    double accumulatedEnergyKwh);
