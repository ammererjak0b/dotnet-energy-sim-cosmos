using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Terminal;
using EnergySim.Time;
using Microsoft.Extensions.Logging;

namespace EnergySim.Time.Cli;

/// <summary>Plain-log mode without a terminal: prints clock changes and the participant table, reads commands from stdin.</summary>
public sealed class TimeServiceHeadless
{
    private const int POLL_MILLISECONDS = 250;
    private const int TABLE_SECONDS = 5;
    private const int FLUSH_MILLISECONDS = 200;
    private const string HELP_TEXT = "commands: start, pause, resume, speed <x>, restart <yyyy-MM-dd HH:mm>, table, quit";

    private readonly TimeAuthority _authority;
    private readonly ClockServiceHost _host;
    private readonly TimeServiceDashboardSource _source;
    private readonly TimeProvider _timeProvider;
    private readonly LogBuffer _logBuffer;
    private readonly PlainConsoleWriter _writer;

    public TimeServiceHeadless(TimeAuthority authority, ClockServiceHost host, TimeServiceDashboardSource source, TimeProvider timeProvider, LogBuffer logBuffer, PlainConsoleWriter writer)
    {
        _authority = authority;
        _host = host;
        _source = source;
        _timeProvider = timeProvider;
        _logBuffer = logBuffer;
        _writer = writer;
    }

    /// <summary>Runs until quit or cancellation; a fault of the printer surfaces. A closed stdin keeps the service running.</summary>
    public async Task runAsync(TextReader input, CancellationToken cancellationToken)
    {
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        writeLine(HELP_TEXT);
        Task writerTask = _writer.runAsync(_logBuffer, TimeSpan.FromMilliseconds(FLUSH_MILLISECONDS), stop.Token); // only writer to stdout
        Task printTask = printStateAsync(stop.Token);
        Task commandTask = readCommandsAsync(input, stop.Token);
        try
        {
            Task finished = await Task.WhenAny(writerTask, printTask, commandTask);
            await finished; // surfaces a fault
        }
        finally
        {
            stop.Cancel();
            await writerTask; // flushes the last lines
        }
    }

    private async Task printStateAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(POLL_MILLISECONDS), _timeProvider);
        ClockSnapshot lastSnapshot = _authority.Clock.getSnapshot();
        long lastTableTimestamp = 0;
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            ClockSnapshot snapshot = _authority.Clock.getSnapshot();
            bool changed = snapshot.Revision != lastSnapshot.Revision || snapshot.Status != lastSnapshot.Status || snapshot.Speed != lastSnapshot.Speed;
            bool tableDue = _timeProvider.GetElapsedTime(lastTableTimestamp).TotalSeconds >= TABLE_SECONDS;
            if (changed || tableDue)
            {
                writeLine(formatClockLine(snapshot));
                writeTable();
                lastSnapshot = snapshot;
                lastTableTimestamp = _timeProvider.GetTimestamp();
            }
        }
    }

    private async Task readCommandsAsync(TextReader input, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await input.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken); // stdin closed: keep serving
                return;
            }

            bool keepRunning = await handleCommandAsync(line.Trim(), cancellationToken);
            if (!keepRunning)
            {
                return;
            }
        }
    }

    /// <summary>Returns false on quit.</summary>
    private async Task<bool> handleCommandAsync(string line, CancellationToken cancellationToken)
    {
        if (line.Length == 0)
        {
            return true;
        }

        string verb = line;
        string argument = string.Empty;
        int separatorIndex = line.IndexOf(' ');
        if (separatorIndex > 0)
        {
            verb = line.Substring(0, separatorIndex);
            argument = line.Substring(separatorIndex + 1).Trim();
        }

        verb = verb.ToLowerInvariant();
        if (verb == "quit")
        {
            writeLine("quit: shutting down");
            return false;
        }

        if (verb == "table")
        {
            writeLine(formatClockLine(_authority.Clock.getSnapshot()));
            writeTable();
            return true;
        }

        CommandResult result = await runClockCommandAsync(verb, argument, cancellationToken);
        writeLine(verb + ": " + result.Message);
        return true;
    }

    private async Task<CommandResult> runClockCommandAsync(string verb, string argument, CancellationToken cancellationToken)
    {
        ClockStatus status = _authority.Clock.getSnapshot().Status;
        if (verb == "start" || verb == "pause" || verb == "resume")
        {
            ClockStatus requiredStatus = ClockStatus.Stopped;
            if (verb == "pause")
            {
                requiredStatus = ClockStatus.Running;
            }
            else if (verb == "resume")
            {
                requiredStatus = ClockStatus.Paused;
            }

            if (status != requiredStatus)
            {
                return new CommandResult(false, "rejected, clock is " + status + ", needs " + requiredStatus);
            }

            return await _source.executeAsync(new DeviceCommand(TimeServiceDashboardSource.COMMAND_TOGGLE, 0), cancellationToken);
        }

        if (verb == "speed")
        {
            double speed;
            if (!double.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
            {
                return new CommandResult(false, "usage: speed <x>");
            }

            return await _source.executeAsync(new DeviceCommand(TimeServiceDashboardSource.COMMAND_SPEED, speed), cancellationToken);
        }

        if (verb == "restart")
        {
            return await _source.executeAsync(new DeviceCommand(TimeServiceDashboardSource.COMMAND_RESTART, argument), cancellationToken);
        }

        return new CommandResult(false, "unknown command. " + HELP_TEXT);
    }

    private static string formatClockLine(ClockSnapshot snapshot)
    {
        return "clock: " + snapshot.Status + " speed " + snapshot.Speed.ToString(CultureInfo.InvariantCulture)
            + " revision " + snapshot.Revision + " generation " + snapshot.Generation + " virtual " + VirtualTimeFormat.format(snapshot.VirtualNow);
    }

    private void writeTable()
    {
        List<ClockParticipantView> views = _host.getParticipantViews();
        writeLine("participants: " + views.Count);
        foreach (ClockParticipantView view in views)
        {
            writeLine("  " + ParticipantFormat.formatLabel(view) + " | " + ParticipantFormat.formatStatus(view));
        }
    }

    private void writeLine(string text)
    {
        _logBuffer.add(LogLineFormat.format(_timeProvider.GetUtcNow(), LogLevel.Information, text));
    }
}
