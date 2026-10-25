using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Terminal;
using EnergySim.Time;
using Microsoft.Extensions.Logging;

namespace EnergySim.Time.Cli;

/// <summary>Maps time-service state and controls onto the shared terminal dashboard.</summary>
public sealed class TimeServiceDashboardSource : IDashboardSource
{
    public const string COMMAND_TOGGLE = "TimeToggle";
    public const string COMMAND_SPEED = "TimeSpeed";
    public const string COMMAND_FASTER = "TimeFaster";
    public const string COMMAND_SLOWER = "TimeSlower";
    public const string COMMAND_RESTART = "TimeRestart";

    private readonly TimeAuthority _authority;
    private readonly ClockServiceHost _host;
    private readonly DateTimeOffset _restartTime;
    private readonly LogBuffer _logBuffer;
    private readonly Action _shutdown;
    private readonly List<CommandDescriptor> _commands;

    public TimeServiceDashboardSource(TimeAuthority authority, ClockServiceHost host, DateTimeOffset restartTime, LogBuffer logBuffer, Action shutdown)
    {
        _authority = authority;
        _host = host;
        _restartTime = restartTime;
        _logBuffer = logBuffer;
        _shutdown = shutdown;
        _commands = createCommands();
    }

    public DashboardState getState()
    {
        ClockSnapshot snapshot = _authority.Clock.getSnapshot();
        DashboardState state = new DashboardState();
        state.DeviceId = "time-service";
        state.DeviceTypeName = "Shared virtual clock";
        state.ClockText = VirtualTimeFormat.format(snapshot.VirtualNow);
        state.Speed = snapshot.Speed;
        state.ClockStatus = snapshot.Status.ToString();
        state.WarningText = "Local Modbus TCP; no internet required";
        if (snapshot.SyncError.Length > 0)
        {
            state.WarningText = snapshot.SyncError;
        }
        state.Commands.AddRange(_commands);
        state.LogLines.AddRange(_logBuffer.getLast(8));
        state.HeaderRows.Add(DeviceSupport.createRow("Session", snapshot.SessionId.ToString("N")));
        state.HeaderRows.Add(DeviceSupport.createRow("Revision", snapshot.Revision.ToString(CultureInfo.InvariantCulture)));
        state.HeaderRows.Add(DeviceSupport.createRow("Generation", snapshot.Generation.ToString(CultureInfo.InvariantCulture)));
        ClockDefinition? pending = _authority.pending();
        if (pending != null)
        {
            state.HeaderRows.Add(DeviceSupport.createRow("Pending effective", VirtualTimeFormat.format(pending.VirtualAnchor)));
        }

        foreach (ClockParticipantView participant in _host.getParticipantViews())
        {
            state.DeviceRows.Add(DeviceSupport.createRow(participant.DeviceId + " @ " + participant.Port, "registered"));
        }

        return state;
    }

    public Task<CommandResult> executeAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.Name == COMMAND_TOGGLE)
            {
                ClockStatus status = _authority.Clock.getSnapshot().Status;
                ClockAction action = ClockAction.Start;
                if (status == ClockStatus.Running)
                {
                    action = ClockAction.Pause;
                }
                else if (status == ClockStatus.Paused)
                {
                    action = ClockAction.Resume;
                }

                _authority.execute(action, _authority.Clock.getSnapshot().Speed, _restartTime);
                return Task.FromResult(new CommandResult(true, action.ToString() + " scheduled."));
            }

            if (command.Name == COMMAND_SPEED)
            {
                _authority.execute(ClockAction.SetSpeed, command.Value, _restartTime);
                return Task.FromResult(new CommandResult(true, "Speed change scheduled."));
            }

            if (command.Name == COMMAND_FASTER || command.Name == COMMAND_SLOWER)
            {
                ClockSnapshot snapshot = _authority.Clock.getSnapshot();
                double factor = 2.0;
                if (command.Name == COMMAND_SLOWER)
                {
                    factor = 0.5;
                }

                _authority.execute(ClockAction.SetSpeed, Math.Clamp(snapshot.Speed * factor, LocalClock.MIN_SPEED, LocalClock.MAX_SPEED), _restartTime);
                return Task.FromResult(new CommandResult(true, "Speed change scheduled."));
            }

            if (command.Name == COMMAND_RESTART)
            {
                _authority.execute(ClockAction.Restart, _authority.current().Speed, _restartTime);
                return Task.FromResult(new CommandResult(true, "Restart scheduled at " + _restartTime.ToString("O", CultureInfo.InvariantCulture) + "."));
            }

            return Task.FromResult(new CommandResult(false, "Unknown clock command."));
        }
        catch (Exception exception) when (exception is InvalidOperationException || exception is ArgumentOutOfRangeException)
        {
            return Task.FromResult(new CommandResult(false, exception.Message));
        }
    }

    public void requestShutdown()
    {
        _shutdown();
    }

    private static List<CommandDescriptor> createCommands()
    {
        List<CommandDescriptor> commands = new List<CommandDescriptor>();
        commands.Add(DeviceSupport.createCommand(COMMAND_TOGGLE, 'P', "Start / pause / resume"));
        commands.Add(DeviceSupport.createParameterCommand(COMMAND_SPEED, 'V', "Set speed", LocalClock.MIN_SPEED, LocalClock.MAX_SPEED, "x"));
        commands.Add(DeviceSupport.createCommand(COMMAND_FASTER, '+', "Speed x2"));
        commands.Add(DeviceSupport.createCommand(COMMAND_SLOWER, '-', "Speed /2"));
        commands.Add(DeviceSupport.createCommand(COMMAND_RESTART, 'R', "Restart at configured start time"));
        return commands;
    }
}
