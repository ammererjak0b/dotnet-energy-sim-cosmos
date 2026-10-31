using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using EnergySim.Core.History;
using EnergySim.Weather;

namespace EnergySim.Tests.Engine;

/// <summary>Records update/reset/command calls. Command "Bad" is rejected.</summary>
public sealed class FakeDevice : ISimulatedDevice
{
    public const string REJECTED_COMMAND = "Bad";

    private readonly object _lock = new object();

    private readonly List<DeviceUpdateCall> _updates = new List<DeviceUpdateCall>();

    private readonly List<DateTimeOffset> _resets = new List<DateTimeOffset>();

    private readonly List<DeviceCommand> _commands = new List<DeviceCommand>();

    private bool _throwOnUpdate;

    public FakeDevice(bool needsWeather)
    {
        NeedsWeather = needsWeather;
    }

    public string DeviceId
    {
        get
        {
            return "fake-1";
        }
    }

    public DeviceType Type
    {
        get
        {
            return DeviceType.Pv;
        }
    }

    public bool NeedsWeather { get; }

    public DeviceHeader Header { get; } = new DeviceHeader();

    public IReadOnlyList<DisplayRow> DisplayRows { get; } = new List<DisplayRow>();

    public IReadOnlyList<CsvHistoryField> CsvHistoryFields { get; } = new List<CsvHistoryField>();

    public IReadOnlyList<CommandDescriptor> Commands { get; } = new List<CommandDescriptor>();

    public bool ThrowOnUpdate
    {
        get
        {
            lock (_lock)
            {
                return _throwOnUpdate;
            }
        }
        set
        {
            lock (_lock)
            {
                _throwOnUpdate = value;
            }
        }
    }

    public List<DeviceUpdateCall> getUpdates()
    {
        lock (_lock)
        {
            return new List<DeviceUpdateCall>(_updates);
        }
    }

    public List<DateTimeOffset> getResets()
    {
        lock (_lock)
        {
            return new List<DateTimeOffset>(_resets);
        }
    }

    public List<DeviceCommand> getCommands()
    {
        lock (_lock)
        {
            return new List<DeviceCommand>(_commands);
        }
    }

    public void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        lock (_lock)
        {
            if (_throwOnUpdate)
            {
                throw new InvalidOperationException("model broken");
            }

            _updates.Add(new DeviceUpdateCall(virtualNow, step, weather));
        }
    }

    public void reset(DateTimeOffset virtualNow)
    {
        lock (_lock)
        {
            _resets.Add(virtualNow);
        }
    }

    public CommandResult applyCommand(DeviceCommand command)
    {
        lock (_lock)
        {
            _commands.Add(command);
        }

        if (command.Name == REJECTED_COMMAND)
        {
            return new CommandResult(false, "value out of range");
        }

        return new CommandResult(true, "ok");
    }
}
