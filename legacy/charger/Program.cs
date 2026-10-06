using LB_01.Application;
using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;

namespace LB_01;

internal class Program
{
    private const int SIMULATION_STEP_MINUTES = 15;
    private const int DISPLAY_DELAY_MILLISECONDS = 1000;
    private const int DEFAULT_CAR_BATTERY_CAPACITY_KWH = 60;
    private const int DEFAULT_CAR_MAXIMUM_POWER_KW = 11;
    private const int DEFAULT_CAR_STATE_OF_CHARGE_PERCENT = 60;

    static void Main(string[] args)
    {
        bool shouldQuit = false;

        while (!shouldQuit)
        {
            SimulationConfiguration configuration = SimulationConfigurationDialog.readConfiguration(
                Console.In,
                Console.Out);
            SimulationSession session = createSession(configuration);
            SimulationController controller = new();
            SimulationParameterConfiguration parameters = new(
                SIMULATION_STEP_MINUTES,
                DISPLAY_DELAY_MILLISECONDS);
            bool pauseMessageDisplayed = false;

            Console.WriteLine("Simulation started. Press P to pause, R to resume, X to restart, Q to quit.");

            while (!controller.shouldQuit && !controller.shouldRestart)
            {
                if (controller.shouldConfigure)
                {
                    parameters.configure(Console.In, Console.Out);
                    controller.completeConfiguration();
                }

                if (controller.shouldAdvance)
                {
                    TimeSpan simulationStep = TimeSpan.FromMinutes(parameters.simulationStepMinutes);
                    Console.Clear();
                    Console.Write(ConsoleDashboard.render(session.advance(simulationStep)));
                    Console.WriteLine();
                    Console.WriteLine($"Step: {parameters.simulationStepMinutes} min | Delay: {parameters.displayDelayMilliseconds} ms");
                    Console.WriteLine("P = pause | R = resume | C = configure | N = new car | A = connect | D = disconnect");
                    Console.WriteLine("L = list cars | E = export CSV | X = restart | Q = quit");
                    pauseMessageDisplayed = false;
                }
                else if (!pauseMessageDisplayed)
                {
                    Console.WriteLine("Simulation paused. Press R to resume, X to restart or Q to quit.");
                    pauseMessageDisplayed = true;
                }
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    controller.processCommand(key.KeyChar);
                    processApplicationCommand(key.KeyChar, session, controller, Console.Out);
                }

                Thread.Sleep(parameters.displayDelayMilliseconds);
            }

            shouldQuit = controller.shouldQuit;
        }
    }

    private static SimulationSession createSession(SimulationConfiguration configuration)
    {
        SimulationSession session = new(configuration.createCharger());

        foreach (ECar car in configuration.cars)
        {
            session.addCar(car);
        }

        return session;
    }

    private static void processApplicationCommand(
        char command,
        SimulationSession session,
        SimulationController controller,
        TextWriter output)
    {
        switch (char.ToLowerInvariant(command))
        {
            case 'n':
                controller.processCommand('p');
                addCar(session, output);
                break;
            case 'a':
                controller.processCommand('p');
                connectCar(session, output);
                break;
            case 'd':
                controller.processCommand('p');
                disconnectCar(session, output);
                break;
            case 'l':
                controller.processCommand('p');
                listCars(session, output);
                break;
            case 'e':
                controller.processCommand('p');
                exportCsv(session, output);
                break;
        }
    }

    private static void addCar(SimulationSession session, TextWriter output)
    {
        int carId = session.cars.Count == 0
            ? 1
            : session.cars.Max(car => car.id) + 1;
        output.Write($"Car name [Car {carId}]: ");
        string carName = Console.ReadLine() ?? $"Car {carId}";
        output.Write($"Battery capacity (kWh) [{DEFAULT_CAR_BATTERY_CAPACITY_KWH}]: ");
        double batteryCapacityKwh = readDouble(DEFAULT_CAR_BATTERY_CAPACITY_KWH);
        output.Write($"Initial SOC (%) [{DEFAULT_CAR_STATE_OF_CHARGE_PERCENT}]: ");
        double stateOfChargePercent = readDouble(DEFAULT_CAR_STATE_OF_CHARGE_PERCENT);
        output.Write($"Maximum charging power (kW) [{DEFAULT_CAR_MAXIMUM_POWER_KW}]: ");
        double maximumChargingPowerKw = readDouble(DEFAULT_CAR_MAXIMUM_POWER_KW);

        ECar car = new()
        {
            id = carId,
            name = string.IsNullOrWhiteSpace(carName) ? $"Car {carId}" : carName,
            batteryCapacityKwh = batteryCapacityKwh,
            stateOfChargePercent = stateOfChargePercent,
            maximumChargingPowerKw = maximumChargingPowerKw
        };
        output.WriteLine(session.addCar(car) ? "Car added." : "Car could not be added.");
    }

    private static void connectCar(SimulationSession session, TextWriter output)
    {
        output.Write("Car ID: ");
        int carId = readInteger();
        output.Write("Port number: ");
        int portNumber = readInteger();
        output.WriteLine(session.connectCar(carId, portNumber)
            ? "Car connected."
            : "Car could not be connected.");
    }

    private static void disconnectCar(SimulationSession session, TextWriter output)
    {
        output.Write("Port number: ");
        int portNumber = readInteger();
        output.WriteLine(session.disconnectCar(portNumber)
            ? "Car disconnected."
            : "Car could not be disconnected.");
    }

    private static void listCars(SimulationSession session, TextWriter output)
    {
        foreach (ECar car in session.cars)
        {
            string connection = car.connectedPort is null
                ? "not connected"
                : $"port {car.connectedPort.portNumber}";
            output.WriteLine($"{car.id}: {car.name}, SOC {car.stateOfChargePercent:F1}%, {connection}");
        }
    }

    private static void exportCsv(SimulationSession session, TextWriter output)
    {
        const string DEFAULT_CSV_PATH = "simulation-data.csv";
        output.Write($"CSV path [{DEFAULT_CSV_PATH}]: ");
        string csvPath = Console.ReadLine() ?? string.Empty;
        csvPath = string.IsNullOrWhiteSpace(csvPath) ? DEFAULT_CSV_PATH : csvPath;

        try
        {
            using StreamWriter file = new(csvPath);
            CsvExporter.write(session.snapshots, file);
            output.WriteLine($"CSV exported to {csvPath}.");
        }
        catch (IOException)
        {
            output.WriteLine($"CSV export failed: the file '{csvPath}' could not be accessed.");
        }
        catch (UnauthorizedAccessException)
        {
            output.WriteLine($"CSV export failed: access to '{csvPath}' was denied.");
        }
        catch (ArgumentException)
        {
            output.WriteLine($"CSV export failed: '{csvPath}' is not a valid file path.");
        }
    }

    private static double readDouble(double defaultValue)
    {
        string? value = Console.ReadLine();
        return double.TryParse(value, out double result) && result >= 0
            ? result
            : defaultValue;
    }

    private static int readInteger()
    {
        return int.TryParse(Console.ReadLine(), out int result) ? result : 0;
    }
}