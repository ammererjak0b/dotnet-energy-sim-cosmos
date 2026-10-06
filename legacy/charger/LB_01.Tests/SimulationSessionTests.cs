using LB_01.src.Application;
using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Snapshots;

namespace LB_01.Tests;

public class SimulationSessionTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "Session Charger";
    private const double PORT_ONE_MAXIMUM_POWER_KW = 11;
    private const double PORT_TWO_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int CAR_ID = 1;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double INITIAL_STATE_OF_CHARGE_PERCENT = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const int SIMULATION_STEP_MINUTES = 15;

    [Fact]
    public void AddsCarAndConnectsItToSelectedPort()
    {
        SimulationSession session = createSession();
        ECar car = createCar(CAR_ID);

        Assert.True(session.addCar(car));
        Assert.True(session.connectCar(CAR_ID, 1));
        Assert.Same(car, session.charger.port1.connectedCar);
    }

    [Fact]
    public void DisconnectsCarFromSelectedPort()
    {
        SimulationSession session = createSession();
        ECar car = createCar(CAR_ID);
        session.addCar(car);
        session.connectCar(CAR_ID, 1);

        Assert.True(session.disconnectCar(1));
        Assert.Null(session.charger.port1.connectedCar);
    }

    [Fact]
    public void AdvanceStoresSnapshotInHistory()
    {
        SimulationSession session = createSession();
        ECar car = createCar(CAR_ID);
        session.addCar(car);
        session.connectCar(CAR_ID, 1);

        SimulationSnapshot snapshot = session.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Single(session.snapshots);
        Assert.Same(snapshot, session.snapshots[0]);
    }

    [Fact]
    public void SessionsHaveIndependentSimulationState()
    {
        SimulationSession firstSession = createSession();
        SimulationSession secondSession = createSession();

        firstSession.advance(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES));

        Assert.Equal(TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES), firstSession.simulation.simulationTime);
        Assert.Equal(TimeSpan.Zero, secondSession.simulation.simulationTime);
        Assert.Empty(secondSession.snapshots);
    }

    private static SimulationSession createSession()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_ONE_MAXIMUM_POWER_KW,
            PORT_TWO_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        return new SimulationSession(charger);
    }

    private static ECar createCar(int id)
    {
        return new ECar
        {
            id = id,
            name = $"Car {id}",
            batteryCapacityKwh = BATTERY_CAPACITY_KWH,
            stateOfChargePercent = INITIAL_STATE_OF_CHARGE_PERCENT,
            maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
        };
    }
}
