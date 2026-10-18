using EnergySim.Core.Devices;
using EnergySim.Core.Weather;

namespace EnergySim.Battery;

/// <summary>
/// Battery as simulated device. Header.EnergyWh = cumulative DISCHARGED energy (generation side) in Wh.
/// Starts On, in auto mode.
/// </summary>
public sealed class BatteryDevice : SimulatedDeviceBase
{
    public const string COMMAND_AUTO_MODE = "AutoMode";
    public const string COMMAND_SET_POWER_KW = "SetPowerKw";
    public const char KEY_AUTO_MODE = 'A';
    public const char KEY_SET_POWER_KW = 'M';

    private const double WATTS_PER_KILOWATT = 1000.0;
    private const double FULL_PERCENT = DeviceSupport.FULL_PERCENT;
    private const ushort FAULT_CODE_OVERTEMPERATURE = 1;

    private readonly BatteryOptions _options;
    private BatteryStorage _storage;
    private bool _autoMode;
    private double _manualPowerKW;

    public BatteryDevice(string deviceId, BatteryOptions options)
        : base(deviceId, DeviceType.Battery, false)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.validate();

        _options = options;
        _storage = new BatteryStorage(options);
        _autoMode = true;
        Header.IsOn = true;

        Commands = createCommands(options);
        refresh();
    }

    public double SocPercent
    {
        get
        {
            return _storage.SocPercent;
        }
    }

    public double TemperatureC
    {
        get
        {
            return _storage.TemperatureC;
        }
    }

    public double ChargedEnergyWh
    {
        get
        {
            return _storage.CumulativeChargedEnergyKWh * WATTS_PER_KILOWATT;
        }
    }

    /// <summary>weather is ignored. Zero step changes nothing.</summary>
    public override void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        if (step < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step), step, "step must not be negative.");
        }

        if (step == TimeSpan.Zero)
        {
            return;
        }

        double requestedPowerKw = 0;
        if (Header.IsOn)
        {
            if (_autoMode)
            {
                requestedPowerKw = BatteryAutoSchedule.getRequestedPowerKW(_options, virtualNow, _storage.SocPercent);
            }
            else
            {
                requestedPowerKw = _manualPowerKW;
            }
        }

        _storage.advance(step, requestedPowerKw);
        refresh();
    }

    public override void reset(DateTimeOffset virtualNow)
    {
        _storage = new BatteryStorage(_options);
        _autoMode = true;
        _manualPowerKW = 0;
        Header.IsOn = true;
        Header.PowerLimitPercent = FULL_PERCENT;
        refresh();
    }

    protected override CommandResult turnOn()
    {
        Header.IsOn = true;
        return new CommandResult(true, "Battery on.");
    }

    protected override CommandResult turnOff()
    {
        Header.IsOn = false;
        _storage.hold();
        return new CommandResult(true, "Battery off.");
    }

    protected override CommandResult triggerFault()
    {
        _storage.triggerOvertemperatureFault();
        return new CommandResult(true, "Overtemperature fault triggered.");
    }

    protected override CommandResult resetFault()
    {
        if (_storage.Fault == BatteryFault.None)
        {
            return new CommandResult(true, "No fault active.");
        }

        if (!_storage.tryResetFault())
        {
            return new CommandResult(false, "Reset refused: cell temperature " + format(_storage.TemperatureC) + " degC, needs <= " + format(_options.ResetTemperatureC) + " degC.");
        }

        return new CommandResult(true, "Fault reset.");
    }

    protected override void setPowerLimit(double percent)
    {
        Header.PowerLimitPercent = percent;
    }

    protected override CommandResult applyDeviceCommand(DeviceCommand command)
    {
        if (command.Name == COMMAND_AUTO_MODE)
        {
            _autoMode = true;
            return new CommandResult(true, "Auto mode (schedule).");
        }

        if (command.Name == COMMAND_SET_POWER_KW)
        {
            return applySetPower(command.Value);
        }

        return new CommandResult(false, "Unknown command: " + command.Name);
    }

    private CommandResult applySetPower(double powerKw)
    {
        double minimumKw = -_options.MaxChargePowerKW;
        double maximumKw = _options.MaxDischargePowerKW;
        if (double.IsNaN(powerKw) || powerKw < minimumKw || powerKw > maximumKw)
        {
            return new CommandResult(false, "Power must be between " + format(minimumKw) + " and " + format(maximumKw) + " kW (negative = charge).");
        }

        _manualPowerKW = powerKw;
        _autoMode = false;
        return new CommandResult(true, "Manual power " + format(powerKw) + " kW.");
    }

    private static List<CommandDescriptor> createCommands(BatteryOptions options)
    {
        List<CommandDescriptor> commands = DeviceSupport.createCommonCommands();
        commands.Add(DeviceSupport.createCommand(COMMAND_AUTO_MODE, KEY_AUTO_MODE, "Auto mode"));
        commands.Add(DeviceSupport.createParameterCommand(COMMAND_SET_POWER_KW, KEY_SET_POWER_KW, "Manual power (- charge, + discharge)", -options.MaxChargePowerKW, options.MaxDischargePowerKW, "kW"));
        return commands;
    }

    /// <summary>Rebuild header and display rows from the storage.</summary>
    protected override void refresh()
    {
        _storage.PowerLimitFactor = Header.PowerLimitPercent / FULL_PERCENT;

        Header.ActivePowerW = (int)Math.Round(_storage.ActualPowerKW * WATTS_PER_KILOWATT);
        Header.EnergyWh = (long)Math.Round(_storage.CumulativeDischargedEnergyKWh * WATTS_PER_KILOWATT);
        Header.FaultCode = 0;
        if (_storage.Fault == BatteryFault.Overtemperature)
        {
            Header.FaultCode = FAULT_CODE_OVERTEMPERATURE;
        }

        Header.State = mapState();
        DisplayRows = createDisplayRows();
    }

    private DeviceState mapState()
    {
        if (_storage.Fault != BatteryFault.None)
        {
            return DeviceState.Fault;
        }

        if (!Header.IsOn)
        {
            return DeviceState.Off;
        }

        if (_storage.ActualPowerKW == 0)
        {
            return DeviceState.Standby;
        }

        if (_storage.IsPowerLimited)
        {
            return DeviceState.Limited;
        }

        return DeviceState.Running;
    }

    private List<DisplayRow> createDisplayRows()
    {
        List<DisplayRow> rows = new List<DisplayRow>();

        double referencePowerKw = Math.Max(_options.MaxChargePowerKW, _options.MaxDischargePowerKW);
        DisplayRow power = DeviceSupport.createRow("Power", format(_storage.ActualPowerKW) + " kW");
        power.HasBar = true;
        power.BarFraction = Math.Clamp(Math.Abs(_storage.ActualPowerKW) / referencePowerKw, 0, 1);
        rows.Add(power);

        DisplayRow soc = DeviceSupport.createRow("SoC", format(_storage.SocPercent) + " %");
        soc.HasBar = true;
        soc.BarFraction = Math.Clamp(_storage.SocPercent / FULL_PERCENT, 0, 1);
        rows.Add(soc);

        DisplayRow temperature = DeviceSupport.createRow("Temperature", format(_storage.TemperatureC) + " degC");
        if (_storage.Fault != BatteryFault.None)
        {
            temperature.Severity = DisplaySeverity.Fault;
        }
        else if (_storage.TemperatureC >= _options.WarningTemperatureC)
        {
            temperature.Severity = DisplaySeverity.Warning;
        }

        rows.Add(temperature);

        rows.Add(DeviceSupport.createRow("Charged", format(_storage.CumulativeChargedEnergyKWh) + " kWh"));
        rows.Add(DeviceSupport.createRow("Discharged", format(_storage.CumulativeDischargedEnergyKWh) + " kWh"));

        string mode = "Manual";
        if (_autoMode)
        {
            mode = "Auto";
        }

        rows.Add(DeviceSupport.createRow("Mode", mode));

        DisplayRow fault = DeviceSupport.createRow("Fault", "None");
        if (_storage.Fault == BatteryFault.Overtemperature)
        {
            fault.Value = "Overtemperature";
            fault.Severity = DisplaySeverity.Fault;
        }

        rows.Add(fault);
        return rows;
    }

    private static string format(double value)
    {
        return DeviceSupport.format(value, "F1");
    }
}
