using BatteryStorageSimulator.Configuration;

namespace BatteryStorageSimulator.Domain;

internal sealed class BatteryStorage
{
    private readonly BatteryOptions options;

    public BatteryStorage(string deviceId, string displayName, BatteryOptions options)
    {
        DeviceId = deviceId;
        DisplayName = displayName;
        this.options = options;

        StoredEnergyKWh = options.NominalCapacityKWh * options.InitialSocPercent / 100.0;
        TemperatureC = options.InitialTemperatureC;
    }

    public string DeviceId { get; }
    public string DisplayName { get; }
    public double StoredEnergyKWh { get; private set; }
    public double RequestedPowerKW { get; private set; }
    public double ActualPowerKW { get; private set; }
    public double TemperatureC { get; private set; }
    public double CumulativeChargedEnergyKWh { get; private set; }
    public double CumulativeDischargedEnergyKWh { get; private set; }
    public BatteryOperatingState OperatingState { get; private set; } = BatteryOperatingState.Standby;
    public BatteryFault Fault { get; private set; } = BatteryFault.None;

    public double SocPercent => StoredEnergyKWh / options.NominalCapacityKWh * 100.0;

    public void Advance(TimeSpan simulatedStep, double requestedPowerKW)
    {
        RequestedPowerKW = requestedPowerKW;

        if (Fault != BatteryFault.None)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Faulted;
            UpdateTemperature(simulatedStep, 0);
            return;
        }

        if (requestedPowerKW > 0)
        {
            Discharge(simulatedStep, requestedPowerKW);
        }
        else if (requestedPowerKW < 0)
        {
            Charge(simulatedStep, Math.Abs(requestedPowerKW));
        }
        else
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
        }

        UpdateTemperature(simulatedStep, Math.Abs(ActualPowerKW));

        if (TemperatureC >= options.FaultTemperatureC)
        {
            Fault = BatteryFault.Overtemperature;
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Faulted;
        }
    }

    public void Hold()
    {
        ActualPowerKW = 0;
        OperatingState = Fault == BatteryFault.None
            ? BatteryOperatingState.Standby
            : BatteryOperatingState.Faulted;
    }

    public void TriggerOvertemperatureFault()
    {
        TemperatureC = Math.Max(TemperatureC, options.FaultTemperatureC + 2);
        Fault = BatteryFault.Overtemperature;
        RequestedPowerKW = 0;
        Hold();
    }

    public bool TryResetFault()
    {
        if (Fault == BatteryFault.None)
        {
            return true;
        }

        if (TemperatureC > options.ResetTemperatureC)
        {
            return false;
        }

        Fault = BatteryFault.None;
        RequestedPowerKW = 0;
        Hold();
        return true;
    }

    public BatterySnapshot CreateSnapshot() => new(
        DeviceId,
        DisplayName,
        options.Technology,
        OperatingState,
        Fault,
        RequestedPowerKW,
        ActualPowerKW,
        StoredEnergyKWh,
        options.NominalCapacityKWh,
        SocPercent,
        TemperatureC,
        options.WarningTemperatureC,
        CumulativeChargedEnergyKWh,
        CumulativeDischargedEnergyKWh);

    private void Charge(TimeSpan step, double requestedChargePowerKW)
    {
        var hours = step.TotalHours;
        var efficiency = options.ChargeEfficiencyPercent / 100.0;
        var maximumStoredEnergy = options.NominalCapacityKWh * options.MaximumSocPercent / 100.0;
        var freeCapacity = Math.Max(0, maximumStoredEnergy - StoredEnergyKWh);
        var maximumPowerFromCapacity = hours > 0 ? freeCapacity / efficiency / hours : 0;
        var inputPower = Math.Min(requestedChargePowerKW, options.MaxChargePowerKW);
        inputPower = Math.Min(inputPower, maximumPowerFromCapacity);

        if (inputPower <= 0.0001)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
            return;
        }

        var inputEnergy = inputPower * hours;
        StoredEnergyKWh += inputEnergy * efficiency;
        StoredEnergyKWh = Math.Min(StoredEnergyKWh, maximumStoredEnergy);
        CumulativeChargedEnergyKWh += inputEnergy;
        ActualPowerKW = -inputPower;
        OperatingState = BatteryOperatingState.Charging;
    }

    private void Discharge(TimeSpan step, double requestedDischargePowerKW)
    {
        var hours = step.TotalHours;
        var efficiency = options.DischargeEfficiencyPercent / 100.0;
        var minimumStoredEnergy = options.NominalCapacityKWh * options.MinimumSocPercent / 100.0;
        var availableEnergy = Math.Max(0, StoredEnergyKWh - minimumStoredEnergy);
        var maximumPowerFromEnergy = hours > 0 ? availableEnergy * efficiency / hours : 0;
        var outputPower = Math.Min(requestedDischargePowerKW, options.MaxDischargePowerKW);
        outputPower = Math.Min(outputPower, maximumPowerFromEnergy);

        if (outputPower <= 0.0001)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
            return;
        }

        var outputEnergy = outputPower * hours;
        StoredEnergyKWh -= outputEnergy / efficiency;
        StoredEnergyKWh = Math.Max(StoredEnergyKWh, minimumStoredEnergy);
        CumulativeDischargedEnergyKWh += outputEnergy;
        ActualPowerKW = outputPower;
        OperatingState = BatteryOperatingState.Discharging;
    }

    private void UpdateTemperature(TimeSpan step, double absolutePowerKW)
    {
        var referencePower = Math.Max(options.MaxChargePowerKW, options.MaxDischargePowerKW);
        var relativePower = referencePower > 0 ? Math.Clamp(absolutePowerKW / referencePower, 0, 1) : 0;
        var targetTemperature = options.AmbientTemperatureC + relativePower * 20.0;
        var response = Math.Clamp(step.TotalHours * 0.5, 0, 1);

        TemperatureC += (targetTemperature - TemperatureC) * response;
    }
}
