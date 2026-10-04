using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Domain.Simulation;

public sealed class Simulation
{
    private const double INITIAL_POWER_KW = 0;
    private const double INITIAL_ENERGY_KWH = 0;
    private readonly DualPortCharger charger;

    public Simulation(DualPortCharger charger)
    {
        this.charger = charger;
    }

    public TimeSpan simulationTime { get; private set; }

    public SimulationSnapshot advance(TimeSpan timeStep)
    {
        simulationTime += timeStep;

        ChargePortSnapshot[] portSnapshots =
        [
            charger.port1.createSnapshot(simulationTime, INITIAL_POWER_KW, INITIAL_ENERGY_KWH),
            charger.port2.createSnapshot(simulationTime, INITIAL_POWER_KW, INITIAL_ENERGY_KWH)
        ];

        return new SimulationSnapshot(simulationTime, portSnapshots);
    }
}
