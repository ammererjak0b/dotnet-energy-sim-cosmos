namespace EnergySim.Battery;

/// <summary>
/// Pure battery physics (port of LB01). Power sign: positive = discharging (generation), negative = charging.
/// </summary>
public sealed class BatteryStorage
{
    private const double MIN_POWER_KW = 0.0001; // below = standby
    private const double TEMPERATURE_RISE_AT_FULL_POWER_C = 20.0; // target = ambient + rel. power * this
    private const double TEMPERATURE_RESPONSE_PER_HOUR = 0.5; // share of the gap closed per hour
    private const double FAULT_TRIGGER_MARGIN_C = 2.0; // manual fault: fault temp + this

    private readonly BatteryOptions _options;

    public BatteryStorage(BatteryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _options = options;
        StoredEnergyKWh = options.CapacityKWh * options.InitialSocPercent / 100.0;
        TemperatureC = options.InitialTemperatureC;
    }

    public double StoredEnergyKWh { get; private set; }

    public double RequestedPowerKW { get; private set; }

    public double ActualPowerKW { get; private set; }

    public double TemperatureC { get; private set; }

    public double CumulativeChargedEnergyKWh { get; private set; }

    public double CumulativeDischargedEnergyKWh { get; private set; }

    public BatteryOperatingState OperatingState { get; private set; } = BatteryOperatingState.Standby;

    public BatteryFault Fault { get; private set; } = BatteryFault.None;

    public double PowerLimitFactor { get; set; } = 1.0; // 0..1, scales max charge/discharge power

    public bool IsPowerLimited { get; private set; } // request exceeded the limited maximum

    public double SocPercent
    {
        get
        {
            return StoredEnergyKWh / _options.CapacityKWh * 100.0;
        }
    }

    /// <summary>Advance by step. requestedPowerKW: positive = discharge, negative = charge.</summary>
    public void advance(TimeSpan step, double requestedPowerKW)
    {
        RequestedPowerKW = requestedPowerKW;
        IsPowerLimited = false;

        if (Fault != BatteryFault.None)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Faulted;
            updateTemperature(step, 0);
            return;
        }

        if (requestedPowerKW > 0)
        {
            discharge(step, requestedPowerKW);
        }
        else if (requestedPowerKW < 0)
        {
            charge(step, Math.Abs(requestedPowerKW));
        }
        else
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
        }

        updateTemperature(step, Math.Abs(ActualPowerKW));

        if (TemperatureC >= _options.FaultTemperatureC)
        {
            Fault = BatteryFault.Overtemperature;
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Faulted;
            IsPowerLimited = false;
        }
    }

    public void hold()
    {
        ActualPowerKW = 0;
        IsPowerLimited = false;
        if (Fault == BatteryFault.None)
        {
            OperatingState = BatteryOperatingState.Standby;
        }
        else
        {
            OperatingState = BatteryOperatingState.Faulted;
        }
    }

    public void triggerOvertemperatureFault()
    {
        TemperatureC = Math.Max(TemperatureC, _options.FaultTemperatureC + FAULT_TRIGGER_MARGIN_C);
        Fault = BatteryFault.Overtemperature;
        RequestedPowerKW = 0;
        hold();
    }

    /// <summary>Reset only at or below ResetTemperatureC. True when no fault remains.</summary>
    public bool tryResetFault()
    {
        if (Fault == BatteryFault.None)
        {
            return true;
        }

        if (TemperatureC > _options.ResetTemperatureC)
        {
            return false;
        }

        Fault = BatteryFault.None;
        RequestedPowerKW = 0;
        hold();
        return true;
    }

    private void charge(TimeSpan step, double requestedChargePowerKW)
    {
        double hours = step.TotalHours;
        double efficiency = _options.ChargeEfficiencyPercent / 100.0;
        double maximumStoredEnergy = _options.CapacityKWh * _options.MaximumSocPercent / 100.0;
        double freeCapacity = Math.Max(0, maximumStoredEnergy - StoredEnergyKWh);
        double maximumPowerFromCapacity = 0;
        if (hours > 0)
        {
            maximumPowerFromCapacity = freeCapacity / efficiency / hours; // grid side power that fills the window
        }

        double maximumPower = _options.MaxChargePowerKW * PowerLimitFactor;
        double inputPower = Math.Min(requestedChargePowerKW, maximumPower);
        inputPower = Math.Min(inputPower, maximumPowerFromCapacity);

        if (inputPower <= MIN_POWER_KW)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
            return;
        }

        double inputEnergy = inputPower * hours; // kWh from grid
        StoredEnergyKWh += inputEnergy * efficiency;
        StoredEnergyKWh = Math.Min(StoredEnergyKWh, maximumStoredEnergy);
        CumulativeChargedEnergyKWh += inputEnergy;
        ActualPowerKW = -inputPower;
        OperatingState = BatteryOperatingState.Charging;
        IsPowerLimited = PowerLimitFactor < 1.0 && requestedChargePowerKW > maximumPower;
    }

    private void discharge(TimeSpan step, double requestedDischargePowerKW)
    {
        double hours = step.TotalHours;
        double efficiency = _options.DischargeEfficiencyPercent / 100.0;
        double minimumStoredEnergy = _options.CapacityKWh * _options.MinimumSocPercent / 100.0;
        double availableEnergy = Math.Max(0, StoredEnergyKWh - minimumStoredEnergy);
        double maximumPowerFromEnergy = 0;
        if (hours > 0)
        {
            maximumPowerFromEnergy = availableEnergy * efficiency / hours; // delivered power that empties the window
        }

        double maximumPower = _options.MaxDischargePowerKW * PowerLimitFactor;
        double outputPower = Math.Min(requestedDischargePowerKW, maximumPower);
        outputPower = Math.Min(outputPower, maximumPowerFromEnergy);

        if (outputPower <= MIN_POWER_KW)
        {
            ActualPowerKW = 0;
            OperatingState = BatteryOperatingState.Standby;
            return;
        }

        double outputEnergy = outputPower * hours; // kWh delivered
        StoredEnergyKWh -= outputEnergy / efficiency;
        StoredEnergyKWh = Math.Max(StoredEnergyKWh, minimumStoredEnergy);
        CumulativeDischargedEnergyKWh += outputEnergy;
        ActualPowerKW = outputPower;
        OperatingState = BatteryOperatingState.Discharging;
        IsPowerLimited = PowerLimitFactor < 1.0 && requestedDischargePowerKW > maximumPower;
    }

    private void updateTemperature(TimeSpan step, double absolutePowerKW)
    {
        double referencePower = Math.Max(_options.MaxChargePowerKW, _options.MaxDischargePowerKW);
        double relativePower = Math.Clamp(absolutePowerKW / referencePower, 0, 1);
        double targetTemperature = _options.AmbientTemperatureC + relativePower * TEMPERATURE_RISE_AT_FULL_POWER_C;
        double response = Math.Clamp(step.TotalHours * TEMPERATURE_RESPONSE_PER_HOUR, 0, 1);

        TemperatureC += (targetTemperature - TemperatureC) * response;
    }
}
