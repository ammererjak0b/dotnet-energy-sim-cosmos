using System.Diagnostics;
using BatteryStorageSimulator.Configuration;
using BatteryStorageSimulator.Domain;
using BatteryStorageSimulator.Persistence;
using BatteryStorageSimulator.Presentation;

namespace BatteryStorageSimulator.Simulation;

internal sealed class SimulationEngine
{
    private readonly SimulatorOptions options;
    private readonly BatteryStorage battery;
    private readonly AutoPowerController autoController;
    private readonly CsvSimulationWriter csvWriter;
    private readonly ConsoleUserInterface userInterface;

    private DateTime simulatedTime;
    private readonly DateTime endTime;
    private TimeSpan step;
    private double speedFactor;
    private double autoPowerLimitPercent;
    private double manualPowerKW;
    private SimulationMode mode = SimulationMode.Auto;
    private bool isPaused;
    private bool exitRequested;
    private string lastEvent = string.Empty;

    public SimulationEngine(
        SimulatorOptions options,
        BatteryStorage battery,
        CsvSimulationWriter csvWriter,
        ConsoleUserInterface userInterface,
        double? speedOverride = null)
    {
        this.options = options;
        this.battery = battery;
        this.csvWriter = csvWriter;
        this.userInterface = userInterface;

        autoController = new AutoPowerController(options.Battery, options.Simulation.RandomSeed);
        simulatedTime = options.Simulation.StartTime;
        endTime = simulatedTime.AddHours(options.Simulation.DurationHours);
        step = TimeSpan.FromMinutes(options.Simulation.StepMinutes);
        speedFactor = speedOverride ?? options.Simulation.SpeedFactor;
        autoPowerLimitPercent = options.Simulation.AutoPowerLimitPercent;
    }

    public void Run()
    {
        RecordEvent("Simulation gestartet");

        while (simulatedTime < endTime && !exitRequested)
        {
            if (WaitForNextStepOrMenu())
            {
                OpenMenu();
                continue;
            }

            var remaining = endTime - simulatedTime;
            var actualStep = remaining < step ? remaining : step;
            var requestedPower = mode == SimulationMode.Auto
                ? autoController.GetRequestedPowerKW(simulatedTime, battery.CreateSnapshot(), autoPowerLimitPercent)
                : manualPowerKW;

            battery.Advance(actualStep, requestedPower);
            simulatedTime += actualStep;

            var status = CreateStatus();
            csvWriter.Write(status);
            userInterface.ShowStatus(status);
        }

        if (!exitRequested)
        {
            battery.Hold();
            RecordEvent("24-Stunden-Simulation abgeschlossen");
            userInterface.ShowCompletion(csvWriter.FilePath);
        }
        else
        {
            RecordEvent("Simulation durch Benutzer beendet");
        }
    }

    private bool WaitForNextStepOrMenu()
    {
        var realDelay = TimeSpan.FromSeconds(step.TotalSeconds / speedFactor);
        if (realDelay <= TimeSpan.Zero)
        {
            return false;
        }

        if (Console.IsInputRedirected)
        {
            if (realDelay.TotalMilliseconds >= 1)
            {
                Thread.Sleep(realDelay);
            }

            return false;
        }

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < realDelay)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.M)
                {
                    return true;
                }
            }

            var remainingMilliseconds = (realDelay - stopwatch.Elapsed).TotalMilliseconds;
            Thread.Sleep((int)Math.Clamp(remainingMilliseconds, 1, 50));
        }

        return false;
    }

    private void OpenMenu()
    {
        isPaused = true;
        battery.Hold();
        RecordEvent("Simulation pausiert");

        while (isPaused && !exitRequested)
        {
            userInterface.ShowMenu();
            var choice = userInterface.ReadMenuChoice();

            switch (choice)
            {
                case 0:
                    exitRequested = true;
                    break;
                case 1:
                    userInterface.ShowStatus(CreateStatus());
                    break;
                case 2:
                    mode = SimulationMode.Auto;
                    RecordEvent("Auto-Modus aktiviert");
                    break;
                case 3:
                    mode = SimulationMode.Manual;
                    manualPowerKW = -userInterface.ReadNumber(
                        "Ladeleistung in kW",
                        0,
                        options.Battery.MaxChargePowerKW);
                    RecordEvent($"Manuelles Laden mit {Math.Abs(manualPowerKW):F2} kW eingestellt");
                    break;
                case 4:
                    mode = SimulationMode.Manual;
                    manualPowerKW = userInterface.ReadNumber(
                        "Entladeleistung in kW",
                        0,
                        options.Battery.MaxDischargePowerKW);
                    RecordEvent($"Manuelles Entladen mit {manualPowerKW:F2} kW eingestellt");
                    break;
                case 5:
                    mode = SimulationMode.Manual;
                    manualPowerKW = 0;
                    RecordEvent("Manuelle Leistung auf 0 kW gesetzt");
                    break;
                case 6:
                    autoPowerLimitPercent = userInterface.ReadNumber(
                        "Auto-Leistungslimit in Prozent",
                        10,
                        100);
                    RecordEvent($"Auto-Leistungslimit auf {autoPowerLimitPercent:F0} Prozent gesetzt");
                    break;
                case 7:
                    speedFactor = userInterface.ReadNumber(
                        "Geschwindigkeitsfaktor",
                        1,
                        1_000_000);
                    RecordEvent($"Geschwindigkeit auf {speedFactor:F0}x gesetzt");
                    break;
                case 8:
                    step = TimeSpan.FromMinutes(userInterface.ReadNumber(
                        "Schrittweite in Minuten",
                        1,
                        60));
                    RecordEvent($"Schrittweite auf {step.TotalMinutes:F0} Minuten gesetzt");
                    break;
                case 9:
                    battery.TriggerOvertemperatureFault();
                    RecordEvent("Übertemperaturstörung manuell ausgelöst");
                    break;
                case 10:
                    if (battery.TryResetFault())
                    {
                        RecordEvent("Störung zurückgesetzt");
                    }
                    else
                    {
                        RecordEvent("Reset abgelehnt: Batterie ist noch zu heiß");
                    }
                    break;
                case 11:
                    isPaused = false;
                    RecordEvent("Simulation fortgesetzt");
                    break;
            }
        }
    }

    private void RecordEvent(string eventText)
    {
        lastEvent = eventText;
        var status = CreateStatus();
        csvWriter.Write(status, eventText);
        userInterface.ShowStatus(status);
    }

    private SimulationStatus CreateStatus() => new(
        simulatedTime,
        endTime,
        step,
        speedFactor,
        mode,
        isPaused,
        autoPowerLimitPercent,
        lastEvent,
        battery.CreateSnapshot());
}
