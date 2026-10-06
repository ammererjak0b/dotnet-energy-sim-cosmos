using LB_01.src.Domain.Entities;

namespace LB_01.src.Application;

public sealed class SimulationConfiguration
{
    private const int CHARGER_ID = 1;
    private const int FIRST_CAR_ID = 1;
    private const double DEFAULT_BATTERY_CAPACITY_KWH = 60;
    private const double DEFAULT_STATE_OF_CHARGE_PERCENT = 60;
    private const double DEFAULT_MAXIMUM_CHARGING_POWER_KW = 11;

    private SimulationConfiguration(
        string chargerName,
        double portOneMaximumPowerKw,
        double portTwoMaximumPowerKw,
        double maximumTotalPowerKw,
        IReadOnlyList<ECar> cars)
    {
        this.chargerName = chargerName;
        this.portOneMaximumPowerKw = portOneMaximumPowerKw;
        this.portTwoMaximumPowerKw = portTwoMaximumPowerKw;
        this.maximumTotalPowerKw = maximumTotalPowerKw;
        this.cars = cars;
    }

    public string chargerName { get; }

    public double portOneMaximumPowerKw { get; }

    public double portTwoMaximumPowerKw { get; }

    public double maximumTotalPowerKw { get; }

    public IReadOnlyList<ECar> cars { get; }

    public static SimulationConfiguration createDefault()
    {
        return create("Demo Charger", 11, 11, 22, 1);
    }

    public static SimulationConfiguration create(
        string chargerName,
        double portOneMaximumPowerKw,
        double portTwoMaximumPowerKw,
        double maximumTotalPowerKw,
        int carCount)
    {
        List<ECar> cars = [];

        for (int carIndex = 0; carIndex < carCount; carIndex++)
        {
            int carId = FIRST_CAR_ID + carIndex;
            cars.Add(createCar(carId, $"Car {carId}", DEFAULT_STATE_OF_CHARGE_PERCENT));
        }

        return new SimulationConfiguration(
            chargerName,
            portOneMaximumPowerKw,
            portTwoMaximumPowerKw,
            maximumTotalPowerKw,
            cars);
    }

    public static SimulationConfiguration create(
        string chargerName,
        double portOneMaximumPowerKw,
        double portTwoMaximumPowerKw,
        double maximumTotalPowerKw,
        IReadOnlyList<ECar> cars)
    {
        return new SimulationConfiguration(
            chargerName,
            portOneMaximumPowerKw,
            portTwoMaximumPowerKw,
            maximumTotalPowerKw,
            cars);
    }

    public DualPortCharger createCharger()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            chargerName,
            portOneMaximumPowerKw,
            portTwoMaximumPowerKw,
            maximumTotalPowerKw);

        if (cars.Count > 0)
        {
            charger.tryConnect(charger.port1, cars[0]);
        }

        if (cars.Count > 1)
        {
            charger.tryConnect(charger.port2, cars[1]);
        }

        return charger;
    }

    private static ECar createCar(int id, string name, double stateOfChargePercent)
    {
        return new ECar
        {
            id = id,
            name = name,
            manufacturer = "Demo Manufacturer",
            model = "Demo Model",
            batteryCapacityKwh = DEFAULT_BATTERY_CAPACITY_KWH,
            stateOfChargePercent = stateOfChargePercent,
            maximumChargingPowerKw = DEFAULT_MAXIMUM_CHARGING_POWER_KW
        };
    }
}
