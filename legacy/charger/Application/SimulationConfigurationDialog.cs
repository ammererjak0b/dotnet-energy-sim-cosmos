using System.Globalization;
using LB_01.Domain.Entities;

namespace LB_01.Application;

public static class SimulationConfigurationDialog
{
    private const int MAXIMUM_CAR_COUNT = 2;
    private const int FIRST_CAR_ID = 1;
    private const double DEFAULT_PORT_ONE_POWER_KW = 11;
    private const double DEFAULT_PORT_TWO_POWER_KW = 11;
    private const double DEFAULT_TOTAL_POWER_KW = 22;
    private const double DEFAULT_BATTERY_CAPACITY_KWH = 60;
    private const double DEFAULT_STATE_OF_CHARGE_PERCENT = 60;
    private const double DEFAULT_MAXIMUM_CHARGING_POWER_KW = 11;

    public static SimulationConfiguration readConfiguration(TextReader input, TextWriter output)
    {
        output.WriteLine("Use default configuration? (Y/n)");
        string? useDefaults = input.ReadLine();

        if (string.IsNullOrWhiteSpace(useDefaults)
            || useDefaults.Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            return SimulationConfiguration.createDefault();
        }

        string chargerName = readText(input, output, "Charger name", "Demo Charger");
        double portOnePowerKw = readDouble(input, output, "Port 1 maximum power (kW)", DEFAULT_PORT_ONE_POWER_KW);
        double portTwoPowerKw = readDouble(input, output, "Port 2 maximum power (kW)", DEFAULT_PORT_TWO_POWER_KW);
        double totalPowerKw = readDouble(input, output, "Charger maximum total power (kW)", DEFAULT_TOTAL_POWER_KW);
        int carCount = readInt(input, output, "Number of cars (0-2)", 1, 0, MAXIMUM_CAR_COUNT);
        List<ECar> cars = [];

        for (int carIndex = 0; carIndex < carCount; carIndex++)
        {
            int carId = FIRST_CAR_ID + carIndex;
            string carName = readText(input, output, $"Car {carId} name", $"Car {carId}");
            double batteryCapacityKwh = readDouble(input, output, $"Car {carId} battery capacity (kWh)", DEFAULT_BATTERY_CAPACITY_KWH);
            double stateOfChargePercent = readDouble(input, output, $"Car {carId} initial SOC (%)", DEFAULT_STATE_OF_CHARGE_PERCENT);
            double maximumChargingPowerKw = readDouble(input, output, $"Car {carId} maximum charging power (kW)", DEFAULT_MAXIMUM_CHARGING_POWER_KW);
            cars.Add(new ECar
            {
                id = carId,
                name = carName,
                manufacturer = "Configured Manufacturer",
                model = "Configured Model",
                batteryCapacityKwh = batteryCapacityKwh,
                stateOfChargePercent = stateOfChargePercent,
                maximumChargingPowerKw = maximumChargingPowerKw
            });
        }

        return SimulationConfiguration.create(
            chargerName,
            portOnePowerKw,
            portTwoPowerKw,
            totalPowerKw,
            cars);
    }

    private static string readText(TextReader input, TextWriter output, string label, string defaultValue)
    {
        output.Write($"{label} [{defaultValue}]: ");
        string? value = input.ReadLine();
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static double readDouble(TextReader input, TextWriter output, string label, double defaultValue)
    {
        while (true)
        {
            string value = readText(input, output, label, defaultValue.ToString(CultureInfo.InvariantCulture));

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                && result >= 0)
            {
                return result;
            }

            output.WriteLine("Please enter a non-negative number.");
        }
    }

    private static int readInt(TextReader input, TextWriter output, string label, int defaultValue, int minimum, int maximum)
    {
        while (true)
        {
            string value = readText(input, output, label, defaultValue.ToString(CultureInfo.InvariantCulture));

            if (int.TryParse(value, CultureInfo.InvariantCulture, out int result)
                && result >= minimum
                && result <= maximum)
            {
                return result;
            }

            output.WriteLine($"Please enter an integer between {minimum} and {maximum}.");
        }
    }
}
