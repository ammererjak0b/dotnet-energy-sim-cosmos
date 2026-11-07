using EnergySim.Core.Devices;
using EnergySim.Core.Weather;

namespace EnergySim.Pv;

/// <summary>Adapter between the PV model and the generic device contract (header, dashboard rows, commands).</summary>
public sealed class PvDevice : SimulatedDeviceBase
{
    public const ushort INVERTER_FAULT_CODE = 1;

    private const double WATTS_PER_KILOWATT = 1000.0;
    private const string NOT_AVAILABLE = "n/a";

    private readonly PvPlant _plant;

    public PvDevice(string deviceId, PvOptions options)
        : base(deviceId, DeviceType.Pv, true)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _plant = new PvPlant(options); // validates options
        Commands = DeviceSupport.createCommonCommands();
        refresh();
    }

    /// <summary>Energy since 00:00 UTC of the current virtual day.</summary>
    public double DailyEnergyWh
    {
        get
        {
            return _plant.EnergyTodayWh;
        }
    }

    public override void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        _plant.update(virtualNow, step, weather);
        refresh();
    }

    public override void reset(DateTimeOffset virtualNow)
    {
        _plant.reset(virtualNow);
        refresh();
    }

    protected override CommandResult turnOn()
    {
        _plant.turnOn();
        return new CommandResult(true, "Plant switched on.");
    }

    protected override CommandResult turnOff()
    {
        _plant.turnOff();
        return new CommandResult(true, "Plant switched off.");
    }

    protected override CommandResult triggerFault()
    {
        _plant.triggerFault();
        return new CommandResult(true, "Inverter fault triggered.");
    }

    protected override CommandResult resetFault()
    {
        if (!_plant.HasFault)
        {
            return new CommandResult(true, "No fault active, nothing to reset.");
        }

        _plant.resetFault();
        return new CommandResult(true, "Fault reset.");
    }

    protected override void setPowerLimit(double percent)
    {
        _plant.setPowerLimit(percent);
    }

    protected override CommandResult applyDeviceCommand(DeviceCommand command)
    {
        return new CommandResult(false, "Unknown command: " + command.Name);
    }

    protected override void refresh()
    {
        Header.State = mapState(_plant.Status);
        Header.FaultCode = 0;
        if (_plant.HasFault)
        {
            Header.FaultCode = INVERTER_FAULT_CODE;
        }

        Header.ActivePowerW = (int)Math.Round(_plant.PowerKw * WATTS_PER_KILOWATT);
        Header.EnergyWh = (long)Math.Round(_plant.TotalEnergyWh);
        Header.IsOn = _plant.IsOn;
        Header.PowerLimitPercent = _plant.PowerLimitPercent;
        DisplayRows = createDisplayRows();
        SpecificValues = createSpecificValues();
    }

    private static DeviceState mapState(PvPlantStatus status)
    {
        if (status == PvPlantStatus.Off)
        {
            return DeviceState.Off;
        }

        if (status == PvPlantStatus.Running)
        {
            return DeviceState.Running;
        }

        if (status == PvPlantStatus.Limited)
        {
            return DeviceState.Limited;
        }

        if (status == PvPlantStatus.Fault)
        {
            return DeviceState.Fault;
        }

        return DeviceState.Standby;
    }

    /// <summary>Order and names as in DeviceProfiles (Pv). Engineering units. 0 while no weather is known.</summary>
    private IReadOnlyList<DeviceValue> createSpecificValues()
    {
        List<DeviceValue> values = new List<DeviceValue>();
        values.Add(new DeviceValue("Irradiance", _plant.IrradianceWm2)); // W/m2
        values.Add(new DeviceValue("ModuleTemperature", _plant.CellTemperatureC)); // degC
        values.Add(new DeviceValue("SunElevation", _plant.SunElevationDeg)); // deg, negative at night
        values.Add(new DeviceValue("DailyEnergy", _plant.EnergyTodayWh)); // Wh since 00:00 UTC
        return values;
    }

    private IReadOnlyList<DisplayRow> createDisplayRows()
    {
        List<DisplayRow> rows = new List<DisplayRow>();

        DisplayRow powerRow = DeviceSupport.createRow("Power", DeviceSupport.format(_plant.PowerKw, "F2") + " kW");
        powerRow.HasBar = true;
        powerRow.BarFraction = Math.Clamp(_plant.PowerKw / _plant.PeakPowerKw, 0.0, 1.0); // vs peak
        rows.Add(powerRow);

        rows.Add(createWeatherRow("Irradiance", DeviceSupport.format(_plant.IrradianceWm2, "F0") + " W/m2"));
        rows.Add(createWeatherRow("Module temperature", DeviceSupport.format(_plant.CellTemperatureC, "F1") + " C"));
        rows.Add(createWeatherRow("Cloud cover", DeviceSupport.format(_plant.CloudCoverPct, "F0") + " %"));
        rows.Add(createWeatherRow("Sun elevation", DeviceSupport.format(_plant.SunElevationDeg, "F1") + " deg"));
        rows.Add(DeviceSupport.createRow("Daily energy", DeviceSupport.format(_plant.EnergyTodayWh / WATTS_PER_KILOWATT, "F2") + " kWh"));

        DisplayRow faultRow = DeviceSupport.createRow("Fault", "none");
        if (_plant.HasFault)
        {
            faultRow.Value = "Inverter fault (code " + INVERTER_FAULT_CODE + ")";
            faultRow.Severity = DisplaySeverity.Fault;
        }

        rows.Add(faultRow);
        return rows;
    }

    private DisplayRow createWeatherRow(string label, string value)
    {
        if (_plant.HasWeather)
        {
            return DeviceSupport.createRow(label, value);
        }

        DisplayRow row = DeviceSupport.createRow(label, NOT_AVAILABLE);
        row.Severity = DisplaySeverity.Warning; // no weather data
        return row;
    }
}
