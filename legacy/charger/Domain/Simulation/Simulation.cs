using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Domain.Simulation;

public sealed class Simulation
{
    private const double InitialPowerKw = 0;
    private const double InitialEnergyKwh = 0;
    private readonly DualPortCharger _charger;

    public Simulation(DualPortCharger charger)
    {
        _charger = charger;
    }

    public TimeSpan SimulationTime { get; private set; }

    public SimulationSnapshot Advance(TimeSpan timeStep)
    {
        SimulationTime += timeStep;

        ChargePortSnapshot[] portSnapshots =
        [
            _charger.Port1.CreateSnapshot(SimulationTime, InitialPowerKw, InitialEnergyKwh),
            _charger.Port2.CreateSnapshot(SimulationTime, InitialPowerKw, InitialEnergyKwh)
        ];

        return new SimulationSnapshot(SimulationTime, portSnapshots);
    }
}
