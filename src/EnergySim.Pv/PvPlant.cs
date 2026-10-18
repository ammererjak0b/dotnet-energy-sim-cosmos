using EnergySim.Core.Weather;

namespace EnergySim.Pv;

/// <summary>Pure PV model: weather in, power and energy out. No I/O, no clock access.</summary>
public class PvPlant
{
    private const double CELL_HEATING_K_PER_WM2 = 0.025; // cell warms ~25 K at 1000 W/m2
    private const double POWER_LOSS_PER_KELVIN = 0.004; // -0.4 % per K above reference
    private const double REFERENCE_CELL_TEMPERATURE_C = 25.0;
    private const double REFERENCE_IRRADIANCE_WM2 = 1000.0;
    private const double WATTS_PER_KILOWATT = 1000.0;
    private const double MAX_LIMIT_PERCENT = 100.0;

    private readonly PvOptions _options;
    private bool _isLimited; // limit cut power in last calculation
    private bool _hasWeather;
    private double _irradianceWm2; // last seen
    private double _ambientTemperatureC;
    private DateTime _currentUtcDay; // for daily energy reset

    public PvPlant(PvOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.validate();
        _options = options;
        reset(DateTimeOffset.UnixEpoch);
    }

    public bool IsOn { get; private set; }

    public bool HasFault { get; private set; }

    public bool HasWeather
    {
        get
        {
            return _hasWeather;
        }
    }

    public double PowerLimitPercent { get; private set; }

    public double PowerKw { get; private set; }

    public double SunElevationDeg { get; private set; }

    public double CloudCoverPct { get; private set; }

    public double IrradianceWm2
    {
        get
        {
            return _irradianceWm2;
        }
    }

    public double PeakPowerKw
    {
        get
        {
            return _options.PeakPowerKw;
        }
    }

    public double EnergyTodayWh { get; private set; }

    public double TotalEnergyWh { get; private set; }

    /// <summary>Ambient + heating by irradiance. 0 while no weather is known.</summary>
    public double CellTemperatureC
    {
        get
        {
            if (!_hasWeather)
            {
                return 0.0;
            }

            return _ambientTemperatureC + CELL_HEATING_K_PER_WM2 * _irradianceWm2;
        }
    }

    public PvPlantStatus Status
    {
        get
        {
            if (HasFault)
            {
                return PvPlantStatus.Fault;
            }

            if (!IsOn)
            {
                return PvPlantStatus.Off;
            }

            if (_irradianceWm2 <= 0.0)
            {
                return PvPlantStatus.Standby;
            }

            if (_isLimited)
            {
                return PvPlantStatus.Limited;
            }

            return PvPlantStatus.Running;
        }
    }

    /// <summary>One tick: take weather (null = unavailable), new power, add energy for the step. Zero step (pause) adds nothing.</summary>
    public void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        if (step < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step), step, "step must not be negative.");
        }

        DateTime utcDay = virtualNow.UtcDateTime.Date;
        if (utcDay != _currentUtcDay)
        {
            EnergyTodayWh = 0.0; // new UTC day
            _currentUtcDay = utcDay;
        }

        takeWeather(weather);
        recalculate();

        double energyWh = PowerKw * WATTS_PER_KILOWATT * step.TotalHours; // kW -> W, times h
        EnergyTodayWh += energyWh;
        TotalEnergyWh += energyWh;
    }

    /// <summary>Back to initial state: on, no fault, limit 100 %, no energy, no weather.</summary>
    public void reset(DateTimeOffset virtualNow)
    {
        IsOn = true;
        HasFault = false;
        PowerLimitPercent = MAX_LIMIT_PERCENT;
        EnergyTodayWh = 0.0;
        TotalEnergyWh = 0.0;
        _currentUtcDay = virtualNow.UtcDateTime.Date;
        takeWeather(null);
        recalculate();
    }

    public void turnOn()
    {
        IsOn = true;
        recalculate();
    }

    public void turnOff()
    {
        IsOn = false;
        recalculate();
    }

    public void triggerFault()
    {
        HasFault = true;
        recalculate();
    }

    public void resetFault()
    {
        HasFault = false;
        recalculate();
    }

    public void setPowerLimit(double percent)
    {
        if (double.IsNaN(percent) || percent < 0.0 || percent > MAX_LIMIT_PERCENT)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Power limit must be between 0 and 100 percent.");
        }

        PowerLimitPercent = percent;
        recalculate();
    }

    private void takeWeather(WeatherDto? weather)
    {
        if (weather == null)
        {
            _hasWeather = false;
            _irradianceWm2 = 0.0;
            _ambientTemperatureC = 0.0;
            SunElevationDeg = 0.0;
            CloudCoverPct = 0.0;
            return;
        }

        _hasWeather = true;
        _irradianceWm2 = Math.Max(0.0, weather.GlobalIrradianceWm2);
        _ambientTemperatureC = weather.TemperatureC;
        SunElevationDeg = weather.SunElevationDeg;
        CloudCoverPct = weather.CloudCoverPct;
    }

    private void recalculate()
    {
        _isLimited = false;
        PowerKw = 0.0;

        if (!IsOn || HasFault || !_hasWeather)
        {
            return;
        }

        double temperatureFactor = 1.0 - POWER_LOSS_PER_KELVIN * (CellTemperatureC - REFERENCE_CELL_TEMPERATURE_C);
        double freePowerKw = _options.PeakPowerKw * (_irradianceWm2 / REFERENCE_IRRADIANCE_WM2) * temperatureFactor * _options.PerformanceRatio;
        freePowerKw = Math.Max(0.0, freePowerKw); // no negative power

        double limitKw = _options.PeakPowerKw * PowerLimitPercent / MAX_LIMIT_PERCENT;
        _isLimited = freePowerKw > limitKw;
        PowerKw = Math.Min(freePowerKw, limitKw);
    }
}
