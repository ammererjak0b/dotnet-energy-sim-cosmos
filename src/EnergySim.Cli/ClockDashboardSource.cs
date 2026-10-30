using EnergySim.Terminal;
using System.Globalization;
using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;

namespace EnergySim.Cli;

/// <summary>
/// Glue between loop and dashboard: receives frames (sink), builds the screen state, routes keys.
/// Clock keys (P V + -) go to the LocalClock, all other commands go to the device through the channel.
/// </summary>
public sealed class ClockDashboardSource : IDashboardSource, ISimulationSink
{
    private const int LOG_LINES_SHOWN = 5;

    private readonly ISimulatedDevice _device;
    private readonly string _locationId;
    private readonly IClockSource _clockSource;

    private readonly LocalClock? _localClock;
    private readonly ChannelWriter<DeviceCommand> _deviceCommands;
    private readonly LogBuffer _logBuffer;
    private readonly Action _shutdown;
    private readonly List<CommandDescriptor> _commands;
    private readonly object _lock = new object();
    private readonly TaskCompletionSource _firstFrame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private SimulationFrame? _lastFrame;

    public ClockDashboardSource(ISimulatedDevice device, string locationId, LocalClock clock, ChannelWriter<DeviceCommand> deviceCommands, LogBuffer logBuffer, Action shutdown)
        : this(device, locationId, clock, clock, deviceCommands, logBuffer, shutdown)
    {
    }

    public ClockDashboardSource(ISimulatedDevice device, string locationId, IClockSource clockSource, LocalClock? localClock, ChannelWriter<DeviceCommand> deviceCommands, LogBuffer logBuffer, Action shutdown)
    {
        _device = device;
        _locationId = locationId;
        _clockSource = clockSource;
        _localClock = localClock;
        _deviceCommands = deviceCommands;
        _logBuffer = logBuffer;
        _shutdown = shutdown;
        _commands = new List<CommandDescriptor>(device.Commands); // device keys first
        if (localClock != null)
        {
            _commands.AddRange(ClockCommands.createDescriptors());
        }
    }

    public Task publishAsync(SimulationFrame frame, CancellationToken ct)
    {
        lock (_lock)
        {
            _lastFrame = frame;
        }

        _firstFrame.TrySetResult();
        return Task.CompletedTask;
    }

    /// <summary>Completes when the loop published its first frame (device is reset to the start time by then).</summary>
    public Task waitForFirstFrameAsync(CancellationToken cancellationToken)
    {
        return _firstFrame.Task.WaitAsync(cancellationToken);
    }

    public DashboardState getState()
    {
        SimulationFrame? frame;
        lock (_lock)
        {
            frame = _lastFrame;
        }

        ClockSnapshot snapshot = _clockSource.getSnapshot(); // live, so keys show at once
        DashboardState state = new DashboardState();
        state.DeviceId = _device.DeviceId;
        state.DeviceTypeName = _device.Type.ToString();
        state.LocationId = _locationId;
        state.ClockText = VirtualTimeFormat.format(snapshot.VirtualNow);
        state.Speed = snapshot.Speed;
        state.ClockStatus = snapshot.Status.ToString();
        state.WarningText = "none";
        state.Commands.AddRange(_commands);
        state.LogLines.AddRange(_logBuffer.getLast(LOG_LINES_SHOWN));
        fillBanner(state, snapshot, frame);
        if (frame != null)
        {
            fillFromFrame(state, frame);
        }

        if (!snapshot.IsSynced && snapshot.SyncError.Length > 0)
        {
            if (state.WarningText == "none")
            {
                state.WarningText = snapshot.SyncError;
            }
            else
            {
                state.WarningText = snapshot.SyncError + " " + state.WarningText;
            }
        }

        return state;
    }

    public Task<CommandResult> executeAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        if (ClockCommands.isClockCommand(command.Name))
        {
            if (_localClock == null)
            {
                return Task.FromResult(new CommandResult(false, "Clock controls belong to the time service."));
            }

            return Task.FromResult(executeClockCommand(command));
        }

        _deviceCommands.TryWrite(command); // channel is unbounded
        return Task.FromResult(new CommandResult(true, string.Empty)); // loop logs the device answer
    }

    public void requestShutdown()
    {
        _shutdown();
    }

    private CommandResult executeClockCommand(DeviceCommand command)
    {
        if (command.Name == ClockCommands.PAUSE)
        {
            return togglePause();
        }

        if (command.Name == ClockCommands.SET_SPEED)
        {
            return setSpeed(command.Value);
        }

        LocalClock clock = getLocalClock();
        if (command.Name == ClockCommands.FASTER)
        {
            return setSpeed(ClockCommands.nextSpeed(clock.getSnapshot().Speed, ClockCommands.fasterFactor()));
        }

        return setSpeed(ClockCommands.nextSpeed(clock.getSnapshot().Speed, ClockCommands.slowerFactor()));
    }

    private CommandResult togglePause()
    {
        LocalClock clock = getLocalClock();
        if (clock.getSnapshot().Status == ClockStatus.Running)
        {
            clock.pause();
            return new CommandResult(true, "Clock paused.");
        }

        clock.resume();
        return new CommandResult(true, "Clock resumed.");
    }

    private CommandResult setSpeed(double speed)
    {
        if (double.IsNaN(speed) || speed < LocalClock.MIN_SPEED || speed > LocalClock.MAX_SPEED)
        {
            return new CommandResult(false, "Speed must be between " + LocalClock.MIN_SPEED + " and " + LocalClock.MAX_SPEED + ".");
        }

        LocalClock clock = getLocalClock();
        clock.setSpeed(speed);
        return new CommandResult(true, "Speed " + speed.ToString("0.##", CultureInfo.InvariantCulture) + "x.");
    }

    private LocalClock getLocalClock()
    {
        if (_localClock == null)
        {
            throw new InvalidOperationException("Clock controls belong to the time service.");
        }

        return _localClock;
    }

    private static void fillBanner(DashboardState state, ClockSnapshot snapshot, SimulationFrame? frame)
    {
        if (frame != null && frame.DeviceError.Length > 0)
        {
            state.BannerText = frame.DeviceError;
            state.BannerSeverity = DisplaySeverity.Fault;
            return;
        }

        if (snapshot.Status == ClockStatus.Paused)
        {
            state.BannerText = "CLOCK PAUSED";
            state.BannerSeverity = DisplaySeverity.Warning;
        }
    }

    private static void fillFromFrame(DashboardState state, SimulationFrame frame)
    {
        if (frame.WeatherStale)
        {
            state.WarningText = "weather: " + frame.WeatherInfo;
        }

        DisplayRow stateRow = DeviceSupport.createRow("State", frame.Header.State.ToString());
        if (frame.Header.State == DeviceState.Fault)
        {
            stateRow.Severity = DisplaySeverity.Fault;
        }
        else if (frame.Header.State == DeviceState.Limited)
        {
            stateRow.Severity = DisplaySeverity.Warning;
        }

        state.HeaderRows.Add(stateRow);
        state.HeaderRows.Add(DeviceSupport.createRow("Active power", DeviceSupport.format(frame.Header.ActivePowerW / 1000.0, "F2") + " kW"));
        state.HeaderRows.Add(DeviceSupport.createRow("Energy", DeviceSupport.format(frame.Header.EnergyWh / 1000.0, "F2") + " kWh"));
        state.HeaderRows.Add(DeviceSupport.createRow("Power limit", DeviceSupport.format(frame.Header.PowerLimitPercent, "0.##") + " %"));
        state.DeviceRows.AddRange(frame.DisplayRows);
    }
}
