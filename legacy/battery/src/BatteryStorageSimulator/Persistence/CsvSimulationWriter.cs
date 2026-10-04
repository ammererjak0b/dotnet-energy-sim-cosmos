using System.Globalization;
using System.Text;
using BatteryStorageSimulator.Configuration;
using BatteryStorageSimulator.Simulation;

namespace BatteryStorageSimulator.Persistence;

internal sealed class CsvSimulationWriter : IDisposable
{
    private readonly StreamWriter? writer;
    private readonly CultureInfo culture;
    private readonly char delimiter;

    public CsvSimulationWriter(CsvOptions options)
    {
        culture = CultureInfo.GetCultureInfo(options.Culture);
        delimiter = options.Delimiter[0];

        if (!options.Enabled)
        {
            return;
        }

        var baseDirectory = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var directory = Path.GetFullPath(Path.Combine(baseDirectory, options.Directory));
        Directory.CreateDirectory(directory);
        FilePath = Path.Combine(directory, options.FileName);

        try
        {
            writer = CreateWriter(FilePath);
        }
        catch (IOException)
        {
            var fileName = Path.GetFileNameWithoutExtension(options.FileName);
            var extension = Path.GetExtension(options.FileName);
            FilePath = Path.Combine(
                directory,
                $"{fileName}-{DateTime.Now:yyyyMMdd-HHmmss}{extension}");
            writer = CreateWriter(FilePath);
        }

        WriteFields(
            "Simulationszeit",
            "Simulationsmodus",
            "Simulation pausiert",
            "Betriebszustand",
            "Angeforderte Leistung kW",
            "Tatsaechliche Leistung kW",
            "Gespeicherte Energie kWh",
            "SOC Prozent",
            "Temperatur C",
            "Geladene Energie kWh",
            "Entladene Energie kWh",
            "Stoerung",
            "Schrittweite Minuten",
            "Geschwindigkeit",
            "Auto-Leistungslimit Prozent",
            "Ereignis");
    }

    public string? FilePath { get; }

    public void Write(SimulationStatus status, string eventText = "")
    {
        if (writer is null)
        {
            return;
        }

        var battery = status.Battery;
        WriteFields(
            status.SimulatedTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            status.Mode.ToString(),
            status.IsPaused ? "ja" : "nein",
            battery.OperatingState.ToString(),
            Format(battery.RequestedPowerKW),
            Format(battery.ActualPowerKW),
            Format(battery.StoredEnergyKWh),
            Format(battery.SocPercent),
            Format(battery.TemperatureC),
            Format(battery.CumulativeChargedEnergyKWh),
            Format(battery.CumulativeDischargedEnergyKWh),
            battery.Fault.ToString(),
            Format(status.Step.TotalMinutes),
            Format(status.SpeedFactor),
            Format(status.AutoPowerLimitPercent),
            eventText);

        writer.Flush();
    }

    public void Dispose() => writer?.Dispose();

    private string Format(double value) => value.ToString("F2", culture);

    private static StreamWriter CreateWriter(string path) =>
        new(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

    private static string? FindRepositoryRoot()
    {
        var startDirectories = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var startDirectory in startDirectories)
        {
            var directory = new DirectoryInfo(startDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "dotnet-energy-sim-lb01.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }

    private void WriteFields(params string[] fields)
    {
        writer?.WriteLine(string.Join(delimiter, fields.Select(Escape)));
    }

    private string Escape(string value)
    {
        if (!value.Contains(delimiter) && !value.Contains('"') &&
            !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
