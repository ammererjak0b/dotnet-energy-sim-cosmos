using EnergySim.Core.Weather;
using EnergySim.Core.History;

namespace EnergySim.Core.Devices;

/// <summary>
/// Shared part of all device adapters: identity, header, common command dispatch (O F L T R), limit range check.
/// The subclass only implements what is different per device.
/// </summary>
public abstract class SimulatedDeviceBase : ISimulatedDevice
{
    protected SimulatedDeviceBase(string deviceId, DeviceType type, bool needsWeather)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("deviceId must not be empty.", nameof(deviceId));
        }

        DeviceId = deviceId;
        Type = type;
        NeedsWeather = needsWeather;
    }

    public string DeviceId { get; }

    public DeviceType Type { get; }

    public bool NeedsWeather { get; }

    public DeviceHeader Header { get; } = new DeviceHeader();

    public IReadOnlyList<DisplayRow> DisplayRows { get; protected set; } = new List<DisplayRow>();

    public IReadOnlyList<CsvHistoryField> CsvHistoryFields { get; protected set; } = new List<CsvHistoryField>();

    public IReadOnlyList<CommandDescriptor> Commands { get; protected set; } = new List<CommandDescriptor>();

    public abstract void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather);

    public abstract void reset(DateTimeOffset virtualNow);

    public CommandResult applyCommand(DeviceCommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        CommandResult result = dispatch(command);
        refresh();
        return result;
    }

    protected abstract CommandResult turnOn();

    protected abstract CommandResult turnOff();

    protected abstract CommandResult triggerFault();

    protected abstract CommandResult resetFault();

    protected abstract void setPowerLimit(double percent); // already range checked

    /// <summary>Device specific commands. Unknown name: return a rejected result.</summary>
    protected abstract CommandResult applyDeviceCommand(DeviceCommand command);

    /// <summary>Rebuild header and display rows from the model.</summary>
    protected abstract void refresh();

    private CommandResult dispatch(DeviceCommand command)
    {
        if (command.Name == CommonCommands.TURN_ON)
        {
            return turnOn();
        }

        if (command.Name == CommonCommands.TURN_OFF)
        {
            return turnOff();
        }

        if (command.Name == CommonCommands.SET_LIMIT)
        {
            return applySetLimit(command.Value);
        }

        if (command.Name == CommonCommands.TRIGGER_FAULT)
        {
            return triggerFault();
        }

        if (command.Name == CommonCommands.RESET_FAULT)
        {
            return resetFault();
        }

        return applyDeviceCommand(command);
    }

    private CommandResult applySetLimit(double percent)
    {
        if (!DeviceSupport.isValidPercent(percent))
        {
            return new CommandResult(false, "Power limit must be between 0 and 100 percent.");
        }

        setPowerLimit(percent);
        return new CommandResult(true, "Power limit set to " + DeviceSupport.format(percent, "F1") + " %.");
    }
}
