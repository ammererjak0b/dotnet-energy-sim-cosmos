using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Simulation;
using LB_01.src.Domain.Snapshots;

namespace LB_01.Tests;

public class SimulationTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int SIMULATION_MINUTES = 15;
    private const double INITIAL_POWER_KW = 0;
    private const double INITIAL_ENERGY_KWH = 0;

    [Fact]
    public void AdvanceIncreasesSimulationTimeAndCreatesSnapshotsForBothPorts()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        Simulation simulation = new(charger);
        TimeSpan simulationStep = TimeSpan.FromMinutes(SIMULATION_MINUTES);

        SimulationSnapshot snapshot = simulation.advance(simulationStep);

        Assert.Equal(simulationStep, simulation.simulationTime);
        Assert.Equal(simulationStep, snapshot.simulationTime);
        Assert.Equal(2, snapshot.ports.Count);
        Assert.Equal(charger.port1.portNumber, snapshot.ports[0].portNumber);
        Assert.Equal(charger.port2.portNumber, snapshot.ports[1].portNumber);
        Assert.Equal(INITIAL_POWER_KW, snapshot.ports[0].currentPowerKw);
        Assert.Equal(INITIAL_ENERGY_KWH, snapshot.ports[0].accumulatedEnergyKwh);
        Assert.Equal(INITIAL_POWER_KW, snapshot.ports[1].currentPowerKw);
        Assert.Equal(INITIAL_ENERGY_KWH, snapshot.ports[1].accumulatedEnergyKwh);
    }
}
