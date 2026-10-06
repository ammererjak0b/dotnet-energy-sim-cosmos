using LB_01.src.Domain.Entities;

namespace LB_01.src.Domain.Snapshots;

public sealed record SimulationSnapshot(
    TimeSpan simulationTime,
    IReadOnlyList<ChargePortSnapshot> ports,
    double totalPowerKw,
    double accumulatedEnergyKwh,
    ChargerStatus chargerStatus,
    string? chargerFault)
{
    public SimulationSnapshot(
        TimeSpan simulationTime,
        IReadOnlyList<ChargePortSnapshot> ports,
        double totalPowerKw,
        double accumulatedEnergyKwh)
        : this(
            simulationTime,
            ports,
            totalPowerKw,
            accumulatedEnergyKwh,
            ChargerStatus.On,
            null)
    {
    }
}
