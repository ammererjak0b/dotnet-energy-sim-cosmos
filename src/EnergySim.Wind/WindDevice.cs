using EnergySim.Core.Devices;
using EnergySim.Core.Weather;

namespace EnergySim.Wind;

/// <summary>Connects the wind model to the shared device header, commands and display.</summary>
public sealed class WindDevice : SimulatedDeviceBase
{
    public const ushort TECHNICAL_FAULT_CODE = 1;

    private const double WATTS_PER_KILOWATT = 1000.0;

    private readonly WindPlant _plant;
    private const double RATED_ROTOR_RPM = 15.0; // demo value, rotor speed linear to wind speed up to rated wind

    private readonly double _ratedPowerKw;
    private readonly double _ratedWindSpeedMs;
    private double _nacelleDirectionDeg; // last valid weather wind direction

    /// <summary>Creates a weather-dependent wind device with validated plant options.</summary>
    public WindDevice(string deviceId, WindOptions options)
        : base(deviceId, DeviceType.Wind, true)
    {
        _plant = new WindPlant(options);
        _ratedPowerKw = options.RatedPowerKw;
        _ratedWindSpeedMs = options.RatedWindSpeedMs;
        Commands = DeviceSupport.createCommonCommands();
        refresh();
    }

    public double WindSpeedMs
    {
        get
        {
            return _plant.WindSpeedMs;
        }
    }

    public double AvailablePowerKw
    {
        get
        {
            return _plant.AvailablePowerKw;
        }
    }

    public WindPlantStatus OperatingStatus
    {
        get
        {
            return _plant.Status;
        }
    }

    /// <summary>Advances the plant using the virtual duration supplied by the shared loop.</summary>
    public override void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        _plant.update(step, weather);
        if (weather != null && _plant.HasWeather)
        {
            _nacelleDirectionDeg = weather.WindDirectionDeg; // nacelle follows wind
        }

        refresh();
    }

    /// <summary>Restores the initial state for a new simulation run.</summary>
    public override void reset(DateTimeOffset virtualNow)
    {
        _plant.reset();
        _nacelleDirectionDeg = 0;
        refresh();
    }

    /// <summary>Enables generation.</summary>
    protected override CommandResult turnOn()
    {
        _plant.turnOn();
        return new CommandResult(true, "Wind plant switched on.");
    }

    /// <summary>Stops generation.</summary>
    protected override CommandResult turnOff()
    {
        _plant.turnOff();
        return new CommandResult(true, "Wind plant switched off.");
    }

    /// <summary>Triggers a technical fault.</summary>
    protected override CommandResult triggerFault()
    {
        _plant.triggerFault();
        return new CommandResult(true, "Wind plant technical fault triggered.");
    }

    /// <summary>Clears a technical fault.</summary>
    protected override CommandResult resetFault()
    {
        _plant.resetFault();
        return new CommandResult(true, "Technical fault reset; storm protection remains active when required.");
    }

    /// <summary>Sets the power ceiling after the shared range check.</summary>
    protected override void setPowerLimit(double percent)
    {
        _plant.setPowerLimit(percent);
    }

    /// <summary>Rejects unsupported device-specific commands.</summary>
    protected override CommandResult applyDeviceCommand(DeviceCommand command)
    {
        return new CommandResult(false, "Unknown command: " + command.Name);
    }

    /// <summary>Copies model values into the common header and display rows.</summary>
    protected override void refresh()
    {
        Header.IsOn = _plant.IsOn;
        Header.PowerLimitPercent = _plant.PowerLimitPercent;
        Header.State = DeviceState.Standby;
        if (_plant.Status == WindPlantStatus.Running)
        {
            Header.State = DeviceState.Running;
        }
        else if (_plant.Status == WindPlantStatus.Off)
        {
            Header.State = DeviceState.Off;
        }
        else if (_plant.Status == WindPlantStatus.Limited)
        {
            Header.State = DeviceState.Limited;
        }
        else if (_plant.Status == WindPlantStatus.Fault)
        {
            Header.State = DeviceState.Fault;
        }

        Header.FaultCode = 0;
        if (_plant.HasFault)
        {
            Header.FaultCode = TECHNICAL_FAULT_CODE;
        }

        Header.ActivePowerW = (int)Math.Round(_plant.PowerKw * WATTS_PER_KILOWATT);
        Header.EnergyWh = (long)Math.Round(_plant.TotalEnergyWh);
        DisplayRows = createDisplayRows();
        SpecificValues = createSpecificValues();
    }

    /// <summary>Order and names as DeviceProfiles Wind, engineering units.</summary>
    private List<DeviceValue> createSpecificValues()
    {
        double rotorSpeedRpm = 0; // standing when no power
        if (_plant.PowerKw > 0)
        {
            rotorSpeedRpm = RATED_ROTOR_RPM * Math.Min(_plant.WindSpeedMs / _ratedWindSpeedMs, 1.0);
        }

        List<DeviceValue> values = new List<DeviceValue>();
        values.Add(new DeviceValue("WindSpeed", _plant.WindSpeedMs)); // m/s
        values.Add(new DeviceValue("RotorSpeed", rotorSpeedRpm)); // rpm, derived
        values.Add(new DeviceValue("NacelleDirection", _nacelleDirectionDeg)); // deg, derived from weather
        return values;
    }

    /// <summary>Builds the fixed display columns also consumed by the common CSV writer.</summary>
    private IReadOnlyList<DisplayRow> createDisplayRows()
    {
        List<DisplayRow> rows = new List<DisplayRow>();
        DisplayRow power = DeviceSupport.createRow("Power", DeviceSupport.format(_plant.PowerKw, "F2") + " kW");
        power.HasBar = true;
        power.BarFraction = Math.Clamp(_plant.PowerKw / _ratedPowerKw, 0.0, 1.0);
        rows.Add(power);
        rows.Add(createWeatherRow("Wind speed", DeviceSupport.format(_plant.WindSpeedMs, "F1") + " m/s"));
        rows.Add(createWeatherRow("Available power", DeviceSupport.format(_plant.AvailablePowerKw, "F2") + " kW"));
        rows.Add(createStatusRow());
        rows.Add(DeviceSupport.createRow("Total energy", DeviceSupport.format(_plant.TotalEnergyWh / WATTS_PER_KILOWATT, "F2") + " kWh"));
        DisplayRow fault = DeviceSupport.createRow("Fault", "none");
        if (_plant.HasFault)
        {
            fault.Value = "Technical fault (code " + TECHNICAL_FAULT_CODE + ")";
            fault.Severity = DisplaySeverity.Fault;
        }

        rows.Add(fault);
        return rows;
    }

    /// <summary>Marks unknown weather values explicitly rather than displaying a false zero.</summary>
    private DisplayRow createWeatherRow(string label, string value)
    {
        if (_plant.HasWeather)
        {
            return DeviceSupport.createRow(label, value);
        }

        DisplayRow row = DeviceSupport.createRow(label, "n/a");
        row.Severity = DisplaySeverity.Warning;
        return row;
    }

    /// <summary>Explains weather and storm protection in the shared dashboard.</summary>
    private DisplayRow createStatusRow()
    {
        DisplayRow row = DeviceSupport.createRow("Status", _plant.Status.ToString());
        if (_plant.Status == WindPlantStatus.WeatherUnavailable)
        {
            row.Value = "Weather unavailable";
            row.Severity = DisplaySeverity.Warning;
        }
        else if (_plant.Status == WindPlantStatus.StormShutdown)
        {
            row.Value = "Storm shutdown";
            row.Severity = DisplaySeverity.Warning;
        }
        else if (_plant.Status == WindPlantStatus.Fault)
        {
            row.Severity = DisplaySeverity.Fault;
        }

        return row;
    }
}
