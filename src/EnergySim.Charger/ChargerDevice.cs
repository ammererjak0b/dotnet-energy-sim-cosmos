using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using EnergySim.Core.History;
using System.Globalization;

namespace EnergySim.Charger;

/// <summary>
/// Dual port EV charger. Consumes power, so Header.ActivePowerW is negative.
/// Fault codes: 1 charger fault, 2 port 1 fault, 3 port 2 fault (charger fault wins, then port 1, then port 2).
/// Device keys: C connect car, D disconnect car, N fault port, E clear port fault (parameter = port 1 or 2).
/// </summary>
public sealed class ChargerDevice : SimulatedDeviceBase
{
    public const ushort FAULT_CODE_CHARGER = 1;
    public const ushort FAULT_CODE_PORT1 = 2;
    public const ushort FAULT_CODE_PORT2 = 3;

    public const string CONNECT_CAR = "ConnectCar";
    public const string DISCONNECT_CAR = "DisconnectCar";
    public const string FAULT_PORT = "FaultPort";
    public const string CLEAR_PORT_FAULT = "ClearPortFault";

    public const char KEY_CONNECT_CAR = 'C';
    public const char KEY_DISCONNECT_CAR = 'D';
    public const char KEY_FAULT_PORT = 'N';
    public const char KEY_CLEAR_PORT_FAULT = 'E';

    public const int PORT_COUNT = 2;

    public const double STATUS_AVAILABLE = 0;
    public const double STATUS_CHARGING = 1;
    public const double STATUS_FAULT = 2;
    public const double STATUS_OFF = 3;

    private const double FULL_SOC_PERCENT = 100.0;
    private const double WATT_PER_KW = 1000.0;
    private const double MINUTES_PER_HOUR = 60.0;
    private const double EPSILON_KW = 0.000001;
    private const double DEFAULT_CAR_CAPACITY_KWH = 60.0;
    private const double DEFAULT_CAR_SOC_PERCENT = 60.0;
    private const double DEFAULT_CAR_MAX_POWER_KW = 11.0;
    private const string DEFAULT_CAR_NAME = "Manual Car";

    private readonly ChargerOptions _options;
    private readonly ChargePort[] _ports;
    private readonly Dictionary<CarScheduleEntry, ECar> _scheduledCars = new Dictionary<CarScheduleEntry, ECar>();
    private bool _isOn = true;
    private bool _chargerFault;
    private double _powerLimitPercent = 100.0;
    private double _consumedEnergyWh;
    private TimeSpan _simulationElapsed;
    private bool _isCurtailed;
    private int _nextCarId = 1;

    public ChargerDevice(string deviceId, ChargerOptions options)
        : base(deviceId, DeviceType.Charger, false)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        validateOptions(options);
        _options = options;
        _ports = new ChargePort[PORT_COUNT];
        _ports[0] = new ChargePort(1, options.Port1MaxPowerKw);
        _ports[1] = new ChargePort(2, options.Port2MaxPowerKw);
        Commands = createCommands();
        refresh();
    }

    /// <summary>Charging power of one port (last step) in kW. 0 for a wrong port number.</summary>
    public double getPortPowerKw(int portNumber)
    {
        ChargePort? port = findPort(portNumber);
        if (port == null)
        {
            return 0;
        }

        return port.PowerKw;
    }

    /// <summary>SoC of the car on the port in percent. 0 when free.</summary>
    public double getPortSocPercent(int portNumber)
    {
        ChargePort? port = findPort(portNumber);
        if (port == null || port.Car == null)
        {
            return 0;
        }

        return port.Car.StateOfChargePercent;
    }

    /// <summary>STATUS_AVAILABLE, STATUS_CHARGING, STATUS_FAULT or STATUS_OFF.</summary>
    public double getPortStatus(int portNumber)
    {
        ChargePort? port = findPort(portNumber);
        if (!isOperational())
        {
            return STATUS_OFF;
        }

        if (port == null || port.HasFault)
        {
            return STATUS_FAULT;
        }

        if (port.PowerKw > 0)
        {
            return STATUS_CHARGING;
        }

        return STATUS_AVAILABLE;
    }

    /// <summary>Schedule first (arrive/leave), then charge for step. virtualNow = time at the end of the step.</summary>
    public override void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        if (step < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Step must not be negative.");
        }

        if (step == TimeSpan.Zero)
        {
            return;
        }

        _simulationElapsed += step;

        applySchedule(virtualNow);
        double[] requestedPowersKw = calculateRequestedPowers();
        double[] assignedPowersKw = splitPower(requestedPowersKw);
        _isCurtailed = sum(requestedPowersKw) > sum(assignedPowersKw) + EPSILON_KW;

        for (int portIndex = 0; portIndex < PORT_COUNT; portIndex++)
        {
            charge(_ports[portIndex], assignedPowersKw[portIndex], step);
        }

        refresh();
    }

    public override void reset(DateTimeOffset virtualNow)
    {
        foreach (ChargePort port in _ports)
        {
            port.Car = null;
            port.HasFault = false;
            port.PowerKw = 0;
            port.EnergyKwh = 0;
        }

        _scheduledCars.Clear();
        _isOn = true;
        _chargerFault = false;
        _powerLimitPercent = 100.0;
        _consumedEnergyWh = 0;
        _simulationElapsed = TimeSpan.Zero;
        _isCurtailed = false;
        _nextCarId = 1;
        applySchedule(virtualNow);
        refresh();
    }

    /// <summary>Plug a car into a free port. False when the port number is wrong, occupied or the car is already plugged.</summary>
    public bool connectCar(int portNumber, ECar car)
    {
        ChargePort? port = findPort(portNumber);
        if (port == null || port.IsOccupied)
        {
            return false;
        }

        foreach (ChargePort otherPort in _ports)
        {
            if (ReferenceEquals(otherPort.Car, car))
            {
                return false;
            }
        }

        port.Car = car;
        if (car.Id <= 0)
        {
            car.Id = _nextCarId;
            _nextCarId++;
        }
        refresh();
        return true;
    }

    protected override CommandResult turnOn()
    {
        _isOn = true;
        return new CommandResult(true, "Charger on");
    }

    protected override CommandResult turnOff()
    {
        _isOn = false;
        return new CommandResult(true, "Charger off");
    }

    protected override CommandResult triggerFault()
    {
        _chargerFault = true;
        return new CommandResult(true, "Charger fault triggered");
    }

    protected override CommandResult resetFault()
    {
        _chargerFault = false;
        foreach (ChargePort port in _ports)
        {
            port.HasFault = false;
        }

        return new CommandResult(true, "Faults cleared");
    }

    protected override void setPowerLimit(double percent)
    {
        _powerLimitPercent = percent;
    }

    protected override CommandResult applyDeviceCommand(DeviceCommand command)
    {
        if (command.Name != CONNECT_CAR && command.Name != DISCONNECT_CAR && command.Name != FAULT_PORT && command.Name != CLEAR_PORT_FAULT)
        {
            return new CommandResult(false, "Unknown command " + command.Name);
        }

        ChargePort? port = null;
        if (command.Value == Math.Floor(command.Value))
        {
            port = findPort((int)command.Value);
        }

        if (port == null)
        {
            return new CommandResult(false, "Port must be 1 or 2");
        }

        if (command.Name == CONNECT_CAR)
        {
            return executeConnectCar(port);
        }

        if (command.Name == DISCONNECT_CAR)
        {
            return executeDisconnectCar(port);
        }

        port.HasFault = command.Name == FAULT_PORT;
        return new CommandResult(true, "Port " + port.PortNumber + " fault " + port.HasFault);
    }

    private CommandResult executeConnectCar(ChargePort port)
    {
        ECar car = new ECar();
        car.Name = DEFAULT_CAR_NAME;
        car.CapacityKwh = DEFAULT_CAR_CAPACITY_KWH;
        car.StateOfChargePercent = DEFAULT_CAR_SOC_PERCENT;
        car.MaxPowerKw = DEFAULT_CAR_MAX_POWER_KW;

        if (!connectCar(port.PortNumber, car))
        {
            return new CommandResult(false, "Port " + port.PortNumber + " is occupied");
        }

        return new CommandResult(true, "Car connected to port " + port.PortNumber);
    }

    private static CommandResult executeDisconnectCar(ChargePort port)
    {
        if (!port.IsOccupied)
        {
            return new CommandResult(false, "Port " + port.PortNumber + " is free");
        }

        port.Car = null;
        port.PowerKw = 0;
        return new CommandResult(true, "Car disconnected from port " + port.PortNumber);
    }

    private ChargePort? findPort(int portNumber)
    {
        foreach (ChargePort port in _ports)
        {
            if (port.PortNumber == portNumber)
            {
                return port;
            }
        }

        return null;
    }

    private bool isOperational()
    {
        return _isOn && !_chargerFault;
    }

    /// <summary>Wanted power per port (port, car and taper limit). 0 when the port cannot charge.</summary>
    private double[] calculateRequestedPowers()
    {
        double[] requestedPowersKw = new double[PORT_COUNT];
        for (int portIndex = 0; portIndex < PORT_COUNT; portIndex++)
        {
            ChargePort port = _ports[portIndex];
            ECar? car = port.Car;
            if (!isOperational() || port.HasFault || car == null || car.StateOfChargePercent >= FULL_SOC_PERCENT)
            {
                continue;
            }

            double maxPowerKw = Math.Min(port.MaxPowerKw, car.MaxPowerKw);
            requestedPowersKw[portIndex] = applyTaper(car.StateOfChargePercent, maxPowerKw);
        }

        return requestedPowersKw;
    }

    private double applyTaper(double socPercent, double maxPowerKw)
    {
        if (socPercent <= _options.TaperStartSocPercent)
        {
            return maxPowerKw;
        }

        double taperFactor = (FULL_SOC_PERCENT - socPercent) / (FULL_SOC_PERCENT - _options.TaperStartSocPercent);
        double taperedPowerKw = Math.Max(_options.MinimumTaperPowerKw, maxPowerKw * taperFactor);
        return Math.Min(maxPowerKw, taperedPowerKw);
    }

    /// <summary>Fair share: equal split of the total limit, ports that need less give the rest back.</summary>
    private double[] splitPower(double[] requestedPowersKw)
    {
        double[] assignedPowersKw = new double[PORT_COUNT];
        List<int> activePorts = new List<int>();
        for (int portIndex = 0; portIndex < PORT_COUNT; portIndex++)
        {
            if (requestedPowersKw[portIndex] > 0)
            {
                activePorts.Add(portIndex);
            }
        }

        double remainingPowerKw = _options.TotalMaxPowerKw * _powerLimitPercent / 100.0;
        while (activePorts.Count > 0 && remainingPowerKw > EPSILON_KW)
        {
            double fairShareKw = remainingPowerKw / activePorts.Count;
            List<int> satisfiedPorts = new List<int>();
            foreach (int portIndex in activePorts)
            {
                double missingKw = requestedPowersKw[portIndex] - assignedPowersKw[portIndex];
                double grantedKw = Math.Min(missingKw, fairShareKw);
                assignedPowersKw[portIndex] += grantedKw;
                remainingPowerKw -= grantedKw;
                if (assignedPowersKw[portIndex] >= requestedPowersKw[portIndex])
                {
                    satisfiedPorts.Add(portIndex);
                }
            }

            foreach (int portIndex in satisfiedPorts)
            {
                activePorts.Remove(portIndex);
            }
        }

        return assignedPowersKw;
    }

    /// <summary>Move energy into the car. Reaching 100 % inside the step shows 0 kW (charging done).</summary>
    private void charge(ChargePort port, double powerKw, TimeSpan step)
    {
        ECar? car = port.Car;
        port.PowerKw = 0;
        if (car == null || powerKw <= 0)
        {
            return;
        }

        double missingEnergyKwh = car.CapacityKwh * (1.0 - car.StateOfChargePercent / 100.0);
        double offeredEnergyKwh = powerKw * step.TotalHours; // kW * h
        double chargedEnergyKwh = Math.Min(offeredEnergyKwh, missingEnergyKwh);
        _consumedEnergyWh += chargedEnergyKwh * WATT_PER_KW;
        port.EnergyKwh += chargedEnergyKwh;

        if (offeredEnergyKwh >= missingEnergyKwh)
        {
            car.StateOfChargePercent = FULL_SOC_PERCENT;
            return;
        }

        car.StateOfChargePercent += chargedEnergyKwh / car.CapacityKwh * 100.0;
        port.PowerKw = powerKw;
    }

    private void applySchedule(DateTimeOffset virtualNow)
    {
        DateTime utcNow = virtualNow.UtcDateTime;
        int minuteOfDay = utcNow.Hour * (int)MINUTES_PER_HOUR + utcNow.Minute;

        foreach (CarScheduleEntry entry in _options.Schedule)
        {
            bool shouldBePresent = isInWindow(entry, minuteOfDay);
            bool isTracked = _scheduledCars.ContainsKey(entry);

            if (shouldBePresent && !isTracked)
            {
                arriveScheduledCar(entry);
            }

            if (!shouldBePresent && isTracked)
            {
                departScheduledCar(entry);
            }
        }
    }

    private void arriveScheduledCar(CarScheduleEntry entry)
    {
        ECar car = new ECar();
        car.Name = entry.Name;
        car.CapacityKwh = entry.CapacityKwh;
        car.StateOfChargePercent = entry.StartSocPercent;
        car.MaxPowerKw = entry.MaxPowerKw;

        if (connectCar(entry.Port, car))
        {
            _scheduledCars[entry] = car; // else port busy: retry next tick
        }
    }

    private void departScheduledCar(CarScheduleEntry entry)
    {
        ECar car = _scheduledCars[entry];
        _scheduledCars.Remove(entry);
        ChargePort? port = findPort(entry.Port);
        if (port != null && ReferenceEquals(port.Car, car))
        {
            port.Car = null;
            port.PowerKw = 0;
        }
    }

    /// <summary>Arrive inclusive, depart exclusive. Depart before arrive = window over midnight.</summary>
    private static bool isInWindow(CarScheduleEntry entry, int minuteOfDay)
    {
        int arriveMinuteOfDay = entry.ArriveHourUtc * (int)MINUTES_PER_HOUR + entry.ArriveMinute;
        int departMinuteOfDay = entry.DepartHourUtc * (int)MINUTES_PER_HOUR + entry.DepartMinute;

        if (arriveMinuteOfDay < departMinuteOfDay)
        {
            return minuteOfDay >= arriveMinuteOfDay && minuteOfDay < departMinuteOfDay;
        }

        if (arriveMinuteOfDay > departMinuteOfDay)
        {
            return minuteOfDay >= arriveMinuteOfDay || minuteOfDay < departMinuteOfDay;
        }

        return false; // arrive == depart: never present
    }

    protected override void refresh()
    {
        double totalPowerKw = 0;
        foreach (ChargePort port in _ports)
        {
            if (!isOperational() || port.HasFault || !port.IsOccupied)
            {
                port.PowerKw = 0;
            }

            totalPowerKw += port.PowerKw;
        }

        if (totalPowerKw <= 0)
        {
            _isCurtailed = false;
        }

        Header.ActivePowerW = -(int)Math.Round(totalPowerKw * WATT_PER_KW); // consumer: negative
        Header.EnergyWh = (long)Math.Round(_consumedEnergyWh);
        Header.IsOn = _isOn;
        Header.PowerLimitPercent = _powerLimitPercent;
        Header.FaultCode = calculateFaultCode();
        Header.State = calculateState(totalPowerKw);
        DisplayRows = createDisplayRows();
        SpecificValues = createSpecificValues();
        CsvHistoryFields = createCsvHistoryFields(totalPowerKw);
    }

    /// <summary>Modbus device block, order as in DeviceProfiles. Port power negative (consumption), SoC 0 when free.</summary>
    private List<DeviceValue> createSpecificValues()
    {
        List<DeviceValue> values = new List<DeviceValue>();
        values.Add(new DeviceValue("Port1Power", 0 - _ports[0].PowerKw * WATT_PER_KW));
        values.Add(new DeviceValue("Port2Power", 0 - _ports[1].PowerKw * WATT_PER_KW));
        values.Add(new DeviceValue("Port1VehicleSoc", getPortSocPercent(1)));
        values.Add(new DeviceValue("Port2VehicleSoc", getPortSocPercent(2)));
        return values;
    }

    private List<CsvHistoryField> createCsvHistoryFields(double totalPowerKw)
    {
        List<CsvHistoryField> fields = new List<CsvHistoryField>();
        fields.Add(new CsvHistoryField("simulationTime", _simulationElapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)));
        fields.Add(new CsvHistoryField("chargerStatus", getChargerStatus()));
        fields.Add(new CsvHistoryField("chargerFault", getChargerFault()));
        foreach (ChargePort port in _ports)
        {
            addPortCsvFields(fields, port);
        }

        fields.Add(new CsvHistoryField("totalPowerKw", formatCsv(totalPowerKw)));
        fields.Add(new CsvHistoryField("totalEnergyKwh", formatCsv(_consumedEnergyWh / WATT_PER_KW)));
        return fields;
    }

    private string getChargerStatus()
    {
        if (_chargerFault)
        {
            return "Fault";
        }

        if (_isOn)
        {
            return "On";
        }

        return "Off";
    }

    private string getChargerFault()
    {
        if (_chargerFault)
        {
            return "Charger fault";
        }

        return string.Empty;
    }

    private void addPortCsvFields(List<CsvHistoryField> fields, ChargePort port)
    {
        string status = "Available";
        if (port.HasFault)
        {
            status = "Fault";
        }
        else if (!_isOn || _chargerFault)
        {
            status = "Off";
        }
        else if (port.PowerKw > 0)
        {
            status = "Charging";
        }

        string carId = string.Empty;
        string carName = string.Empty;
        string soc = string.Empty;
        if (port.Car != null)
        {
            carId = port.Car.Id.ToString(CultureInfo.InvariantCulture);
            carName = port.Car.Name;
            soc = formatCsv(port.Car.StateOfChargePercent);
        }

        fields.Add(new CsvHistoryField("port" + port.PortNumber + "Status", status));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "Fault", port.HasFault ? "Port fault" : string.Empty));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "CarId", carId));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "CarName", carName));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "PowerKw", formatCsv(port.PowerKw)));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "EnergyKwh", formatCsv(port.EnergyKwh)));
        fields.Add(new CsvHistoryField("port" + port.PortNumber + "SocPercent", soc));
    }

    private static string formatCsv(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private ushort calculateFaultCode()
    {
        if (_chargerFault)
        {
            return FAULT_CODE_CHARGER;
        }

        if (_ports[0].HasFault)
        {
            return FAULT_CODE_PORT1;
        }

        if (_ports[1].HasFault)
        {
            return FAULT_CODE_PORT2;
        }

        return 0;
    }

    private DeviceState calculateState(double totalPowerKw)
    {
        if (_chargerFault)
        {
            return DeviceState.Fault;
        }

        if (!_isOn)
        {
            return DeviceState.Off;
        }

        if (totalPowerKw <= 0)
        {
            return DeviceState.Standby;
        }

        if (_isCurtailed)
        {
            return DeviceState.Limited;
        }

        return DeviceState.Running;
    }

    private List<DisplayRow> createDisplayRows()
    {
        List<DisplayRow> rows = new List<DisplayRow>();
        foreach (ChargePort port in _ports)
        {
            rows.Add(createPortRow(port));
        }

        rows.Add(DeviceSupport.createRow("Charging power", formatNumber(-Header.ActivePowerW / WATT_PER_KW) + " kW"));
        rows.Add(DeviceSupport.createRow("Energy consumed", formatNumber(_consumedEnergyWh / WATT_PER_KW) + " kWh"));
        return rows;
    }

    private DisplayRow createPortRow(ChargePort port)
    {
        DisplayRow row = DeviceSupport.createRow("Port " + port.PortNumber, "free");

        if (port.Car != null)
        {
            row.Label = row.Label + ": " + port.Car.Name;
            row.Value = formatNumber(port.Car.StateOfChargePercent) + " % / " + formatNumber(port.PowerKw) + " kW";
            row.HasBar = true;
            row.BarFraction = port.Car.StateOfChargePercent / 100.0;
        }

        if (port.HasFault)
        {
            row.Value = "fault";
            row.Severity = DisplaySeverity.Fault;
        }

        return row;
    }

    private static string formatNumber(double value)
    {
        return DeviceSupport.format(value, "0.0");
    }

    private static double sum(double[] values)
    {
        double total = 0;
        foreach (double value in values)
        {
            total += value;
        }

        return total;
    }

    private static List<CommandDescriptor> createCommands()
    {
        List<CommandDescriptor> commands = DeviceSupport.createCommonCommands();
        commands.Add(DeviceSupport.createParameterCommand(CONNECT_CAR, KEY_CONNECT_CAR, "Connect car", 1, PORT_COUNT, "port"));
        commands.Add(DeviceSupport.createParameterCommand(DISCONNECT_CAR, KEY_DISCONNECT_CAR, "Disconnect car", 1, PORT_COUNT, "port"));
        commands.Add(DeviceSupport.createParameterCommand(FAULT_PORT, KEY_FAULT_PORT, "Fault port", 1, PORT_COUNT, "port"));
        commands.Add(DeviceSupport.createParameterCommand(CLEAR_PORT_FAULT, KEY_CLEAR_PORT_FAULT, "Clear port fault", 1, PORT_COUNT, "port"));
        return commands;
    }

    private static void validateOptions(ChargerOptions options)
    {
        if (options.Port1MaxPowerKw <= 0 || options.Port2MaxPowerKw <= 0 || options.TotalMaxPowerKw <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Power limits must be positive.");
        }

        if (options.TaperStartSocPercent < 0 || options.TaperStartSocPercent >= FULL_SOC_PERCENT)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Taper start must be 0..<100 %.");
        }

        if (options.MinimumTaperPowerKw < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Minimum taper power must not be negative.");
        }

        if (options.Schedule == null)
        {
            throw new ArgumentNullException(nameof(options), "Schedule must not be null.");
        }

        foreach (CarScheduleEntry entry in options.Schedule)
        {
            validateEntry(entry);
        }
    }

    private static void validateEntry(CarScheduleEntry entry)
    {
        if (entry.Port < 1 || entry.Port > PORT_COUNT)
        {
            throw new ArgumentOutOfRangeException(nameof(entry), "Schedule port must be 1 or 2.");
        }

        bool isTimeValid = entry.ArriveHourUtc >= 0 && entry.ArriveHourUtc <= 23
            && entry.DepartHourUtc >= 0 && entry.DepartHourUtc <= 23
            && entry.ArriveMinute >= 0 && entry.ArriveMinute <= 59
            && entry.DepartMinute >= 0 && entry.DepartMinute <= 59;
        if (!isTimeValid)
        {
            throw new ArgumentOutOfRangeException(nameof(entry), "Schedule time must be 00:00..23:59.");
        }

        bool isCarValid = entry.CapacityKwh > 0 && entry.MaxPowerKw > 0
            && entry.StartSocPercent >= 0 && entry.StartSocPercent <= FULL_SOC_PERCENT;
        if (!isCarValid)
        {
            throw new ArgumentOutOfRangeException(nameof(entry), "Schedule car values out of range.");
        }
    }
}
