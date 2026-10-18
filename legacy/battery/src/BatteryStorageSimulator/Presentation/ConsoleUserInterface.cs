using System.Globalization;
using System.Text;
using BatteryStorageSimulator.Domain;
using BatteryStorageSimulator.Simulation;

namespace BatteryStorageSimulator.Presentation;

internal sealed class ConsoleUserInterface
{
    private readonly int socBarWidth;
    private bool frameInitialized;
    private int statusLineCount;
    private int menuAreaLineCount;

    public ConsoleUserInterface(int socBarWidth)
    {
        this.socBarWidth = socBarWidth;
        Console.OutputEncoding = Encoding.UTF8;
    }

    public void ShowStatus(SimulationStatus status)
    {
        var battery = status.Battery;
        var simulationState = status.IsPaused ? "PAUSIERT" : "LÄUFT";
        var temperatureNote = battery.TemperatureC >= battery.WarningTemperatureC
            ? "  WARNUNG: hohe Temperatur"
            : string.Empty;

        var lines = new List<string>
        {
            $"=== {battery.DisplayName} ({battery.DeviceId}) | {status.Mode.ToString().ToUpperInvariant()} | {simulationState} ===",
            $"Technologie:       {battery.Technology}",
            $"Simulationszeit:   {status.SimulatedTime:yyyy-MM-dd HH:mm} / {status.EndTime:yyyy-MM-dd HH:mm}",
            $"Schritt / Tempo:   {status.Step.TotalMinutes:F0} min / {status.SpeedFactor:F0}x",
            $"Auto-Limit:        {status.AutoPowerLimitPercent:F0} %",
            string.Empty,
            $"Betriebszustand:   {Translate(battery.OperatingState)}",
            $"Soll-Leistung:     {battery.RequestedPowerKW,8:F2} kW",
            $"Ist-Leistung:      {battery.ActualPowerKW,8:F2} kW  (+ Entladen / - Laden)",
            $"Gespeicherte Energie: {battery.StoredEnergyKWh,7:F2} / {battery.NominalCapacityKWh:F2} kWh",
            $"SOC:               {battery.SocPercent,7:F2} %  {CreateBar(battery.SocPercent)}",
            $"Temperatur:        {battery.TemperatureC,7:F2} °C{temperatureNote}",
            $"Geladen gesamt:    {battery.CumulativeChargedEnergyKWh,7:F2} kWh",
            $"Entladen gesamt:   {battery.CumulativeDischargedEnergyKWh,7:F2} kWh",
            $"Störung:           {Translate(battery.Fault)}"
        };

        if (!string.IsNullOrWhiteSpace(status.LastEvent))
        {
            lines.Add($"Letztes Ereignis:  {status.LastEvent}");
        }

        lines.Add(string.Empty);
        lines.Add("Während der Simulation: M drücken, um zu pausieren und das Menü zu öffnen.");

        if (Console.IsOutputRedirected)
        {
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            return;
        }

        RenderStatusFrame(lines, status.IsPaused);
    }

    public void ShowMenu()
    {
        var lines = new[]
        {
            string.Empty,
            "--- Steuerungsmenü (Simulation ist pausiert) ---",
            " 1 - Status erneut anzeigen",
            " 2 - Auto-Modus aktivieren",
            " 3 - Manuell laden",
            " 4 - Manuell entladen",
            " 5 - Manuelle Leistung auf 0 kW setzen",
            " 6 - Auto-Leistungslimit ändern",
            " 7 - Simulationsgeschwindigkeit ändern",
            " 8 - Schrittweite ändern",
            " 9 - Übertemperaturstörung auslösen",
            "10 - Störung zurücksetzen",
            "11 - Simulation fortsetzen",
            " 0 - Programm beenden"
        };

        if (!Console.IsOutputRedirected)
        {
            Console.CursorVisible = true;
        }

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        menuAreaLineCount = 1;
    }

    public int ReadMenuChoice()
    {
        while (true)
        {
            Console.Write("Auswahl: ");
            if (int.TryParse(Console.ReadLine(), out var choice) && choice is >= 0 and <= 11)
            {
                return choice;
            }

            Console.WriteLine("Bitte eine Zahl zwischen 0 und 11 eingeben.");
        }
    }

    public double ReadNumber(string prompt, double minimum, double maximum)
    {
        while (true)
        {
            Console.Write($"{prompt} ({minimum:F0} bis {maximum:F0}): ");
            var input = Console.ReadLine();

            if (TryParseNumber(input, out var value) && value >= minimum && value <= maximum)
            {
                return value;
            }

            Console.WriteLine("Ungültige Eingabe. Bitte einen Wert im angegebenen Bereich eingeben.");
        }
    }

    public void ShowCompletion(string? csvPath)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.CursorVisible = true;
            Console.SetCursorPosition(0, statusLineCount);
        }

        Console.WriteLine();
        Console.WriteLine("Die vollständige 24-Stunden-Simulation wurde abgeschlossen.");
        if (!string.IsNullOrWhiteSpace(csvPath))
        {
            Console.WriteLine($"CSV-Verlauf: {csvPath}");
        }
    }

    private string CreateBar(double percentage)
    {
        var filled = (int)Math.Round(Math.Clamp(percentage, 0, 100) / 100.0 * socBarWidth);
        return $"[{new string('█', filled)}{new string('░', socBarWidth - filled)}]";
    }

    private void RenderStatusFrame(IReadOnlyList<string> lines, bool isPaused)
    {
        try
        {
            if (!frameInitialized || menuAreaLineCount > 0)
            {
                Console.Clear();
                frameInitialized = true;
                menuAreaLineCount = 0;
            }

            Console.CursorVisible = false;
            var width = Math.Max(20, Console.WindowWidth - 1);
            var rowsToRender = Math.Max(statusLineCount, lines.Count);

            for (var index = 0; index < rowsToRender; index++)
            {
                RenderLine(index, index < lines.Count ? lines[index] : string.Empty, width);
            }

            statusLineCount = lines.Count;

            Console.SetCursorPosition(0, statusLineCount);
            Console.CursorVisible = isPaused;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            frameInitialized = false;
            Console.Clear();
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }

            statusLineCount = lines.Count;
        }
    }

    private static void RenderLine(int row, string text, int width)
    {
        var visibleText = text.Length > width ? text[..width] : text;
        Console.SetCursorPosition(0, row);
        Console.Write(visibleText.PadRight(width));
    }

    private static string Translate(BatteryOperatingState state) => state switch
    {
        BatteryOperatingState.Charging => "Lädt",
        BatteryOperatingState.Discharging => "Entlädt",
        BatteryOperatingState.Faulted => "STÖRUNG",
        _ => "Bereitschaft"
    };

    private static string Translate(BatteryFault fault) => fault switch
    {
        BatteryFault.Overtemperature => "Übertemperatur",
        _ => "Keine"
    };

    private static bool TryParseNumber(string? input, out double value) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
        double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
