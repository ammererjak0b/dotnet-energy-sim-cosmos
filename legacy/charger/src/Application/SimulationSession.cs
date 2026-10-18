using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Simulation;
using LB_01.src.Domain.Snapshots;

namespace LB_01.src.Application;

public sealed class SimulationSession
{
    private readonly Dictionary<int, ECar> carsById = [];

    public SimulationSession(DualPortCharger charger)
    {
        this.charger = charger;
        simulation = new Simulation(charger);
    }

    public DualPortCharger charger { get; }

    public Simulation simulation { get; }

    public IReadOnlyCollection<ECar> cars => carsById.Values;

    public IReadOnlyList<SimulationSnapshot> snapshots => snapshotHistory;

    private List<SimulationSnapshot> snapshotHistory { get; } = [];

    public bool addCar(ECar car)
    {
        if (carsById.ContainsKey(car.id))
        {
            return false;
        }

        carsById.Add(car.id, car);
        return true;
    }

    public bool connectCar(int carId, int portNumber)
    {
        if (!carsById.TryGetValue(carId, out ECar? car))
        {
            return false;
        }

        ChargePort? port = getPort(portNumber);
        return port is not null && charger.tryConnect(port, car);
    }

    public bool disconnectCar(int portNumber)
    {
        ChargePort? port = getPort(portNumber);
        return port is not null && charger.tryDisconnect(port);
    }

    public SimulationSnapshot advance(TimeSpan timeStep)
    {
        SimulationSnapshot snapshot = simulation.advance(timeStep);
        snapshotHistory.Add(snapshot);
        return snapshot;
    }

    private ChargePort? getPort(int portNumber)
    {
        return portNumber switch
        {
            1 => charger.port1,
            2 => charger.port2,
            _ => null
        };
    }
}
