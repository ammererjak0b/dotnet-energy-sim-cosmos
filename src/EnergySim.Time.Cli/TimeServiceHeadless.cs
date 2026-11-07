using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Terminal;
using EnergySim.Time;

namespace EnergySim.Time.Cli;

/// <summary>Plain-log mode without a terminal: prints clock changes and the participant table, reads commands from stdin.</summary>
public sealed class TimeServiceHeadless
{
    private const int POLL_MILLISECONDS = 250;
    private const int TABLE_SECONDS = 5;
    private const string HELP_TEXT = "commands: start, pause, resume, speed <x>, restart <yyyy-MM-dd HH:mm>, table, quit";

    private readonly TimeAuthority _authority;
    private readonly ClockServiceHost _host;
    private readonly TimeServiceDashboardSource _source;
    private readonly TimeProvider _timeProvider;

    public TimeServiceHeadless(TimeAuthority authority, ClockServiceHost host, TimeServiceDashboardSource source, TimeProvider timeProvider)
    {
        _authority = authority;
        _host = host;
        _source = source;
        _timeProvider = timeProvider;
    }

    /// <summary>Runs until quit, end of cancellation. A closed stdin keeps the service running.</summary>
    public async Task runAsync(TextReader input, CancellationToken cancellationToken)
    {
        await writeLineAsync(HELP_TEXT, cancellationToken);
        Task printTask = printStateAsync(cancellationToken);
        Task commandTask = readCommandsAsync(input, cancellationToken);
        await Task.WhenAny(printTask, commandTask);
    }

    private async Task printStateAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(POLL_MILLISECONDS), _timeProvider);
        string lastClockLine = string.Empty;
        long lastTableTimestamp = 0;
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            string clockLine = formatClockLine(true);
            bool changed = clockLine != lastClockLine;
            bool tableDue = _timeProvider.GetElapsedTime(lastTableTimestamp).TotalSeconds >= TABLE_SECONDS;
            if (changed || tableDue)
            {
                await writeLineAsync(formatClockLine(false), cancellationToken);
                await printTableAsync(cancellationToken);
                lastClockLine = clockLine;
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
            await writeLineAsync("quit: shutting down", cancellationToken);
            return false;
        }

        if (verb == "table")
        {
            await writeLineAsync(formatClockLine(false), cancellationToken);
            await printTableAsync(cancellationToken);
            return true;
        }

        CommandResult result = await runClockCommandAsync(verb, argument, cancellationToken);
        await writeLineAsync(verb + ": " + result.Message, cancellationToken);
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

    private string formatClockLine(bool forChangeDetection)
    {
        ClockSnapshot snapshot = _authority.Clock.getSnapshot();
        string line = "clock: " + snapshot.Status + " speed " + snapshot.Speed.ToString(CultureInfo.InvariantCulture)
            + " revision " + snapshot.Revision + " generation " + snapshot.Generation;
        if (!forChangeDetection)
        {
            line += " virtual " + VirtualTimeFormat.format(snapshot.VirtualNow); // ticking value, not part of the change key
        }

        return line;
    }

    private async Task printTableAsync(CancellationToken cancellationToken)
    {
        List<ClockParticipantView> views = _host.getParticipantViews();
        await writeLineAsync("participants: " + views.Count, cancellationToken);
        foreach (ClockParticipantView view in views)
        {
            await writeLineAsync("  " + ParticipantFormat.formatLabel(view) + " | " + ParticipantFormat.formatStatus(view), cancellationToken);
        }
    }

    private async Task writeLineAsync(string text, CancellationToken cancellationToken)
    {
        string stamp = _timeProvider.GetUtcNow().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        await Console.Out.WriteLineAsync((stamp + "Z " + text).AsMemory(), cancellationToken);
    }
}
