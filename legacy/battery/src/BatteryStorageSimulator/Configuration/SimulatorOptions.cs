using System.Text.Json;

namespace BatteryStorageSimulator.Configuration;

internal sealed class SimulatorOptions
{
    public string DeviceId { get; init; } = "BAT-01";
    public string DisplayName { get; init; } = "Batteriespeicher 01";
    public BatteryOptions Battery { get; init; } = new();
    public SimulationOptions Simulation { get; init; } = new();
    public OutputOptions Output { get; init; } = new();
    public CsvOptions Csv { get; init; } = new();

    public static SimulatorOptions Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Konfigurationsdatei wurde nicht gefunden.", path);
        }

        var json = File.ReadAllText(path);
        var options = JsonSerializer.Deserialize<SimulatorOptions>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Die Konfigurationsdatei ist leer oder ungueltig.");

        options.Validate();
        return options;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId) || string.IsNullOrWhiteSpace(DisplayName))
        {
            throw new InvalidOperationException("Geraete-ID und Anzeigename duerfen nicht leer sein.");
        }

        Battery.Validate();
        Simulation.Validate();
        Output.Validate();
        Csv.Validate();
    }
}

internal sealed class BatteryOptions
{
    public string Technology { get; init; } = "LFP";
    public double NominalCapacityKWh { get; init; } = 100;
    public double MaxChargePowerKW { get; init; } = 50;
    public double MaxDischargePowerKW { get; init; } = 50;
    public double InitialSocPercent { get; init; } = 50;
    public double MinimumSocPercent { get; init; } = 10;
    public double MaximumSocPercent { get; init; } = 90;
    public double ChargeEfficiencyPercent { get; init; } = 95;
    public double DischargeEfficiencyPercent { get; init; } = 95;
    public double InitialTemperatureC { get; init; } = 25;
    public double AmbientTemperatureC { get; init; } = 22;
    public double WarningTemperatureC { get; init; } = 40;
    public double FaultTemperatureC { get; init; } = 50;
    public double ResetTemperatureC { get; init; } = 40;

    public void Validate()
    {
        if (NominalCapacityKWh <= 0 || MaxChargePowerKW <= 0 || MaxDischargePowerKW <= 0)
        {
            throw new InvalidOperationException("Kapazitaet und Leistungsgrenzen muessen groesser als 0 sein.");
        }

        if (MinimumSocPercent < 0 || MaximumSocPercent > 100 || MinimumSocPercent >= MaximumSocPercent)
        {
            throw new InvalidOperationException("Die SOC-Grenzen muessen zwischen 0 und 100 liegen.");
        }

        if (InitialSocPercent < MinimumSocPercent || InitialSocPercent > MaximumSocPercent)
        {
            throw new InvalidOperationException("Der Start-SOC muss innerhalb der SOC-Grenzen liegen.");
        }

        if (ChargeEfficiencyPercent <= 0 || ChargeEfficiencyPercent > 100 ||
            DischargeEfficiencyPercent <= 0 || DischargeEfficiencyPercent > 100)
        {
            throw new InvalidOperationException("Wirkungsgrade muessen groesser als 0 und hoechstens 100 Prozent sein.");
        }

        if (ResetTemperatureC >= FaultTemperatureC || WarningTemperatureC > FaultTemperatureC)
        {
            throw new InvalidOperationException("Die Temperaturgrenzen sind nicht plausibel angeordnet.");
        }
    }
}

internal sealed class SimulationOptions
{
    public DateTime StartTime { get; init; } = new(2026, 7, 15);
    public double DurationHours { get; init; } = 24;
    public double StepMinutes { get; init; } = 15;
    public double SpeedFactor { get; init; } = 600;
    public int RandomSeed { get; init; } = 42;
    public double AutoPowerLimitPercent { get; init; } = 100;

    public void Validate()
    {
        if (DurationHours < 24)
        {
            throw new InvalidOperationException("Die Simulation muss mindestens 24 Stunden umfassen.");
        }

        if (StepMinutes <= 0 || StepMinutes > 60)
        {
            throw new InvalidOperationException("Die Schrittweite muss zwischen 0 und 60 Minuten liegen.");
        }

        if (SpeedFactor <= 0)
        {
            throw new InvalidOperationException("Der Geschwindigkeitsfaktor muss groesser als 0 sein.");
        }

        if (AutoPowerLimitPercent < 10 || AutoPowerLimitPercent > 100)
        {
            throw new InvalidOperationException("Das Auto-Leistungslimit muss zwischen 10 und 100 Prozent liegen.");
        }
    }
}

internal sealed class OutputOptions
{
    public int SocBarWidth { get; init; } = 20;

    public void Validate()
    {
        if (SocBarWidth < 10 || SocBarWidth > 50)
        {
            throw new InvalidOperationException("Die Breite des SOC-Balkens muss zwischen 10 und 50 liegen.");
        }
    }
}

internal sealed class CsvOptions
{
    public bool Enabled { get; init; } = true;
    public string Directory { get; init; } = "data";
    public string FileName { get; init; } = "battery-run.csv";
    public string Delimiter { get; init; } = ";";
    public string Culture { get; init; } = "de-AT";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Directory) || string.IsNullOrWhiteSpace(FileName))
        {
            throw new InvalidOperationException("CSV-Ordner und Dateiname duerfen nicht leer sein.");
        }

        if (Delimiter.Length != 1)
        {
            throw new InvalidOperationException("Das CSV-Trennzeichen muss genau ein Zeichen lang sein.");
        }

        try
        {
            _ = System.Globalization.CultureInfo.GetCultureInfo(Culture);
        }
        catch (System.Globalization.CultureNotFoundException exception)
        {
            throw new InvalidOperationException("Die CSV-Kultur ist ungueltig.", exception);
        }
    }
}
