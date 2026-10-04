using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class SimulationTests
{
    private const int ChargerId = 1;
    private const string ChargerName = "charger-1";
    private const double PortMaximumPowerKw = 11;
    private const double MaximumTotalPowerKw = 22;
    private const int SimulationMinutes = 15;
    private const double InitialPowerKw = 0;
    private const double InitialEnergyKwh = 0;

    [Fact]
    public void AdvanceIncreasesSimulationTimeAndCreatesSnapshotsForBothPorts()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        Simulation simulation = new(charger);
        TimeSpan simulationStep = TimeSpan.FromMinutes(SimulationMinutes);

        SimulationSnapshot snapshot = simulation.Advance(simulationStep);

        Assert.Equal(simulationStep, simulation.SimulationTime);
        Assert.Equal(simulationStep, snapshot.SimulationTime);
        Assert.Equal(2, snapshot.Ports.Count);
        Assert.Equal(charger.Port1.PortNumber, snapshot.Ports[0].PortNumber);
        Assert.Equal(charger.Port2.PortNumber, snapshot.Ports[1].PortNumber);
        Assert.Equal(InitialPowerKw, snapshot.Ports[0].CurrentPowerKw);
        Assert.Equal(InitialEnergyKwh, snapshot.Ports[0].AccumulatedEnergyKwh);
        Assert.Equal(InitialPowerKw, snapshot.Ports[1].CurrentPowerKw);
        Assert.Equal(InitialEnergyKwh, snapshot.Ports[1].AccumulatedEnergyKwh);
    }
}
