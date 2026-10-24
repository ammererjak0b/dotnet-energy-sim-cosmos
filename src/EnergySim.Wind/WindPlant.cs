using EnergySim.Core.Weather;

namespace EnergySim.Wind;

/// <summary>Pure wind plant model without I/O or access to a real or virtual clock.</summary>
public sealed class WindPlant
{
    private const double FULL_PERCENT = 100.0;
    private const double WATTS_PER_KILOWATT = 1000.0;

    private readonly WindOptions _options;

    /// <summary>Creates a plant from a private snapshot of valid configuration values.</summary>
    public WindPlant(WindOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.validate();
        _options = copyOptions(options);
        reset();
    }

    public bool IsOn { get; private set; }

    public bool HasFault { get; private set; }

    public bool HasWeather { get; private set; }

    public bool IsStormShutdown { get; private set; }

    public double PowerLimitPercent { get; private set; }

    public double PowerKw { get; private set; }

    public double WindSpeedMs { get; private set; }

    public double AvailablePowerKw { get; private set; }

    public double TotalEnergyWh { get; private set; }

    public WindPlantStatus Status { get; private set; }

    /// <summary>Stops electrical generation without resetting energy.</summary>
    public void turnOff()
    {
        IsOn = false;
        recalculate();
    }

    /// <summary>Enables generation when weather and protection allow it.</summary>
    public void turnOn()
    {
        IsOn = true;
        recalculate();
    }

    /// <summary>Sets the electrical power ceiling as a percentage of rated power.</summary>
    public void setPowerLimit(double percent)
    {
        if (!double.IsFinite(percent) || percent < 0.0 || percent > FULL_PERCENT)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Power limit must be finite and between 0 and 100 percent.");
        }

        PowerLimitPercent = percent;
        recalculate();
    }

    /// <summary>Triggers a technical fault without changing storm protection.</summary>
    public void triggerFault()
    {
        HasFault = true;
        recalculate();
    }

    /// <summary>Clears a technical fault without bypassing weather or storm protection.</summary>
    public void resetFault()
    {
        HasFault = false;
        recalculate();
    }

    /// <summary>Restores the initial state for a new simulation run.</summary>
    public void reset()
    {
        IsOn = true;
        HasFault = false;
        HasWeather = false;
        IsStormShutdown = false;
        PowerLimitPercent = FULL_PERCENT;
        TotalEnergyWh = 0.0;
        WindSpeedMs = 0.0;
        recalculate();
    }

    /// <summary>Stores a private snapshot of the validated plant configuration.</summary>
    private static WindOptions copyOptions(WindOptions options)
    {
        WindOptions copy = new WindOptions();
        copy.RatedPowerKw = options.RatedPowerKw;
        copy.CutInWindSpeedMs = options.CutInWindSpeedMs;
        copy.RatedWindSpeedMs = options.RatedWindSpeedMs;
        copy.CutOutWindSpeedMs = options.CutOutWindSpeedMs;
        copy.RestartWindSpeedMs = options.RestartWindSpeedMs;
        return copy;
    }

    /// <summary>Applies weather for the supplied virtual duration.</summary>
    public void update(TimeSpan step, WeatherDto? weather)
    {
        if (step < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step), step, "step must not be negative.");
        }

        takeWeather(weather);
        recalculate();
        TotalEnergyWh += PowerKw * WATTS_PER_KILOWATT * step.TotalHours; // kW -> W, times h
    }

    /// <summary>Accepts finite nonnegative wind and preserves storm protection if weather is unavailable.</summary>
    private void takeWeather(WeatherDto? weather)
    {
        if (weather == null || !double.IsFinite(weather.WindSpeedMs) || weather.WindSpeedMs < 0.0)
        {
            HasWeather = false;
            WindSpeedMs = 0.0;
            return;
        }

        HasWeather = true;
        WindSpeedMs = weather.WindSpeedMs;
        updateStormProtection();
    }

    /// <summary>Refreshes power and operating state without advancing accumulated energy.</summary>
    private void recalculate()
    {
        AvailablePowerKw = 0.0;
        PowerKw = 0.0;
        if (HasWeather)
        {
            AvailablePowerKw = calculateAvailablePowerKw();
        }

        if (HasFault)
        {
            Status = WindPlantStatus.Fault;
            return;
        }

        if (!IsOn)
        {
            Status = WindPlantStatus.Off;
            return;
        }

        if (!HasWeather)
        {
            Status = WindPlantStatus.WeatherUnavailable;
            return;
        }

        if (IsStormShutdown)
        {
            Status = WindPlantStatus.StormShutdown;
            return;
        }

        double limitKw = _options.RatedPowerKw * PowerLimitPercent / FULL_PERCENT;
        PowerKw = Math.Min(AvailablePowerKw, limitKw);
        Status = WindPlantStatus.Standby;
        if (AvailablePowerKw > 0.0)
        {
            Status = WindPlantStatus.Running;
            if (PowerKw < AvailablePowerKw)
            {
                Status = WindPlantStatus.Limited;
            }
        }
    }

    /// <summary>Latches storm shutdown until valid wind falls below the restart threshold.</summary>
    private void updateStormProtection()
    {
        if (WindSpeedMs >= _options.CutOutWindSpeedMs)
        {
            IsStormShutdown = true;
        }
        else if (WindSpeedMs < _options.RestartWindSpeedMs)
        {
            IsStormShutdown = false;
        }
    }

    /// <summary>Calculates the normalized cubic power curve between cut-in and rated speed.</summary>
    private double calculateAvailablePowerKw()
    {
        if (WindSpeedMs >= _options.CutOutWindSpeedMs)
        {
            return 0.0;
        }

        if (WindSpeedMs <= _options.CutInWindSpeedMs)
        {
            return 0.0;
        }

        if (WindSpeedMs >= _options.RatedWindSpeedMs)
        {
            return _options.RatedPowerKw;
        }

        double normalizedWind = WindSpeedMs / _options.RatedWindSpeedMs;
        double normalizedCutIn = _options.CutInWindSpeedMs / _options.RatedWindSpeedMs;
        double rangeFraction = (WindSpeedMs - _options.CutInWindSpeedMs) / (_options.RatedWindSpeedMs - _options.CutInWindSpeedMs);
        double numerator = normalizedWind * normalizedWind + normalizedWind * normalizedCutIn + normalizedCutIn * normalizedCutIn;
        double denominator = 1.0 + normalizedCutIn + normalizedCutIn * normalizedCutIn;
        double powerFraction = rangeFraction * numerator / denominator; // (v^3 - cutIn^3) / (rated^3 - cutIn^3)
        return _options.RatedPowerKw * powerFraction;
    }
}
