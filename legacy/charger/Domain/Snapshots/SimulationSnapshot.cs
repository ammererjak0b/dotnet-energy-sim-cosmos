namespace LB_01.Domain.Snapshots;

public sealed record SimulationSnapshot(
    TimeSpan SimulationTime,
    IReadOnlyList<ChargePortSnapshot> Ports);
