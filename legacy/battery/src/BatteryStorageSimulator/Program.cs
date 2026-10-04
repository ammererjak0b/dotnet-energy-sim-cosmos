namespace BatteryStorageSimulator;

using System.Text.Json;
using BatteryStorageSimulator.Configuration;
using BatteryStorageSimulator.Domain;
using BatteryStorageSimulator.Persistence;
using BatteryStorageSimulator.Presentation;
using BatteryStorageSimulator.Simulation;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var commandLine = ParseArguments(args);
            if (commandLine.ShowHelp)
            {
                ShowHelp();
                return 0;
            }

            Console.Title = "Batteriespeicher-Simulator";

            var configPath = commandLine.ConfigPath ??
                Path.Combine(AppContext.BaseDirectory, "batterysettings.json");
            var options = SimulatorOptions.Load(configPath);
            var battery = new BatteryStorage(options.DeviceId, options.DisplayName, options.Battery);
            var userInterface = new ConsoleUserInterface(options.Output.SocBarWidth);
            using var csvWriter = new CsvSimulationWriter(options.Csv);

            var engine = new SimulationEngine(
                options,
                battery,
                csvWriter,
                userInterface,
                commandLine.SpeedOverride);
            engine.Run();

            return 0;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            JsonException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"Fehler beim Programmstart: {exception.Message}");
            return 1;
        }
    }

    private static CommandLineOptions ParseArguments(string[] args)
    {
        string? configPath = null;
        double? speedOverride = null;
        var showHelp = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--config" when index + 1 < args.Length:
                    configPath = args[++index];
                    break;
                case "--speed" when index + 1 < args.Length:
                    if (!double.TryParse(
                            args[++index],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var speed) || speed <= 0)
                    {
                        throw new ArgumentException("Der Wert fuer --speed muss groesser als 0 sein.");
                    }

                    speedOverride = speed;
                    break;
                case "--help" or "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unbekanntes Argument: {args[index]}");
            }
        }

        return new CommandLineOptions(configPath, speedOverride, showHelp);
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Batteriespeicher-Simulator");
        Console.WriteLine("  --config <pfad>  Alternative Konfigurationsdatei verwenden");
        Console.WriteLine("  --speed <faktor> Geschwindigkeit fuer diesen Start ueberschreiben");
        Console.WriteLine("  --help           Diese Hilfe anzeigen");
    }

    private sealed record CommandLineOptions(
        string? ConfigPath,
        double? SpeedOverride,
        bool ShowHelp);
}
