using LB_01.src.Domain.Simulation;
using LB_01.src.Application;
using LB_01.src.Domain.Entities;

namespace LB_01.src;

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
                    Console.WriteLine("P = pause | R = resume | O = charger on/off | F = fault management");
                    Console.WriteLine("C = configure | N = new car | A = connect | D = disconnect");
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
            case 'o':
                controller.processCommand('p');
                toggleCharger(session, output);
                break;
            case 'f':
                controller.processCommand('p');
                configureFaults(session, output);
                break;
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

    private static void toggleCharger(SimulationSession session, TextWriter output)
    {
        if (session.charger.status == ChargerStatus.Off)
        {
            session.charger.turnOn();
        }
        else if (session.charger.status == ChargerStatus.On)
        {
            session.charger.turnOff();
        }

        output.WriteLine($"Charger status: {session.charger.status.ToString().ToUpperInvariant()}");
    }

    private static void configureFaults(SimulationSession session, TextWriter output)
    {
        output.WriteLine("Fault management:");
        output.WriteLine("1 = set charger fault");
        output.WriteLine("2 = set port 1 fault");
        output.WriteLine("3 = set port 2 fault");
        output.WriteLine("4 = clear charger fault");
        output.WriteLine("5 = clear port 1 fault");
        output.WriteLine("6 = clear port 2 fault");
        output.WriteLine("0 = back");
        output.Write("Selection: ");
        string? selection = Console.ReadLine();

        switch (selection)
        {
            case "1":
                session.charger.setFault(readFaultDescription());
                break;
            case "2":
                session.charger.port1.setFault(readFaultDescription());
                break;
            case "3":
                session.charger.port2.setFault(readFaultDescription());
                break;
            case "4":
                session.charger.clearFault();
                break;
            case "5":
                session.charger.port1.clearFault();
                break;
            case "6":
                session.charger.port2.clearFault();
                break;
            case "0":
                return;
            default:
                output.WriteLine("Unknown fault selection.");
                return;
        }

        output.WriteLine("Fault configuration updated.");
    }

    private static string readFaultDescription()
    {
        Console.Write("Fault description [manual fault]: ");
        string? description = Console.ReadLine();
        return string.IsNullOrWhiteSpace(description) ? "manual fault" : description.Trim();
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
        const string DEFAULT_CSV_FILE_NAME = "simulation-data.csv";
        string dataDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "data"));
        output.Write($"CSV file name [{DEFAULT_CSV_FILE_NAME}]: ");
        string csvFileName = Console.ReadLine() ?? string.Empty;
        csvFileName = string.IsNullOrWhiteSpace(csvFileName)
            ? DEFAULT_CSV_FILE_NAME
            : Path.GetFileName(csvFileName);
        string csvPath = Path.Combine(dataDirectory, csvFileName);

        try
        {
            Directory.CreateDirectory(dataDirectory);
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