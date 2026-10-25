using System.Globalization;
using EnergySim.Core.Devices;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EnergySim.Terminal;

/// <summary>
/// Shared device screen: status header, values table, log panel, key legend.
/// Live display, keys polled without blocking, parameter commands via modal prompt (Live is paused meanwhile).
/// </summary>
public sealed class DeviceDashboard
{
    public static readonly TimeSpan DEFAULT_REFRESH_INTERVAL = TimeSpan.FromMilliseconds(250);

    private const char QUIT_KEY = 'Q';

    private readonly IAnsiConsole _console;
    private readonly IKeySource _keySource;
    private readonly TimeProvider _timeProvider;
    private readonly LogBuffer _logBuffer;
    private readonly TimeSpan _refreshInterval;
    private CommandDescriptor? _pendingCommand; // parameter command waiting for the prompt
    private bool _quitRequested;

    public DeviceDashboard(IAnsiConsole console, IKeySource keySource, TimeProvider timeProvider, LogBuffer logBuffer)
        : this(console, keySource, timeProvider, logBuffer, DEFAULT_REFRESH_INTERVAL)
    {
    }

    public DeviceDashboard(IAnsiConsole console, IKeySource keySource, TimeProvider timeProvider, LogBuffer logBuffer, TimeSpan refreshInterval)
    {
        _console = console;
        _keySource = keySource;
        _timeProvider = timeProvider;
        _logBuffer = logBuffer;
        _refreshInterval = refreshInterval;
    }

    /// <summary>Runs until quit key or cancellation. Cancellation ends it normally (no exception).</summary>
    public async Task runAsync(IDashboardSource source, CancellationToken cancellationToken)
    {
        _quitRequested = false;
        _pendingCommand = null;
        try
        {
            while (!_quitRequested && !cancellationToken.IsCancellationRequested)
            {
                LiveDisplay live = _console.Live(buildLayout(source.getState()));
                live.AutoClear(false);
                await live.StartAsync(context => runLiveAsync(context, source, cancellationToken));

                if (_pendingCommand != null)
                {
                    CommandDescriptor command = _pendingCommand;
                    _pendingCommand = null;
                    _console.Clear(); // drop the frozen live frame
                    await runParameterCommandAsync(source, command, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // normal end
        }
    }

    public static IRenderable buildLayout(DashboardState state)
    {
        List<string> infoLines = new List<string>();
        string speedText = state.Speed.ToString("0.##", CultureInfo.InvariantCulture);
        infoLines.Add("Time: " + state.ClockText + "   Speed: " + speedText + "x   Clock: " + state.ClockStatus);
        infoLines.Add("Warnings: " + state.WarningText);

        List<DisplayRow> allRows = new List<DisplayRow>(state.HeaderRows);
        allRows.AddRange(state.DeviceRows);

        string title = state.DeviceTypeName + " " + state.DeviceId;
        if (state.LocationId.Length > 0)
        {
            title = title + " @ " + state.LocationId;
        }

        List<IRenderable> parts = new List<IRenderable>();
        parts.Add(TerminalBlocks.statusHeader(title, infoLines, state.BannerText, state.BannerSeverity));
        parts.Add(TerminalBlocks.keyValueTable(allRows));
        parts.Add(TerminalBlocks.logPanel(state.LogLines));
        parts.Add(TerminalBlocks.keyLegend(state.Commands));
        return new Rows(parts);
    }

    private async Task runLiveAsync(LiveDisplayContext context, IDashboardSource source, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            context.UpdateTarget(buildLayout(source.getState()));
            context.Refresh();

            bool leaveLive = await handleKeysAsync(source, cancellationToken);
            if (leaveLive)
            {
                return;
            }

            await Task.Delay(_refreshInterval, _timeProvider, cancellationToken);
        }
    }

    // true = leave the live display (quit or prompt needed)
    private async Task<bool> handleKeysAsync(IDashboardSource source, CancellationToken cancellationToken)
    {
        char key;
        while (_keySource.tryReadKey(out key))
        {
            char upperKey = char.ToUpperInvariant(key);
            CommandDescriptor? command = findCommand(source.getState(), upperKey);

            if (command == null && upperKey == QUIT_KEY)
            {
                _quitRequested = true;
                source.requestShutdown();
                return true;
            }

            if (command == null)
            {
                continue; // unknown key
            }

            if (command.HasParameter)
            {
                _pendingCommand = command;
                return true;
            }

            await executeAndLogAsync(source, new DeviceCommand(command.Name, 0), cancellationToken);
        }

        return false;
    }

    private static CommandDescriptor? findCommand(DashboardState state, char upperKey)
    {
        foreach (CommandDescriptor command in state.Commands)
        {
            if (char.ToUpperInvariant(command.Key) == upperKey)
            {
                return command;
            }
        }

        return null;
    }

    private async Task runParameterCommandAsync(IDashboardSource source, CommandDescriptor command, CancellationToken cancellationToken)
    {
        if (command.IsTextParameter)
        {
            await runTextParameterCommandAsync(source, command, cancellationToken);
            return;
        }

        string range = formatNumber(command.Minimum) + " and " + formatNumber(command.Maximum);
        TextPrompt<string> prompt = new TextPrompt<string>(Markup.Escape(command.Label + " (" + range + " " + command.Unit + ", empty = cancel)"));
        prompt.AllowEmpty = true;
        prompt.Validator = text => validateParameter(text, command, range);

        string answer = await prompt.ShowAsync(_console, cancellationToken);
        if (answer.Trim().Length == 0)
        {
            return; // cancelled
        }

        double value = parseNumber(answer);
        await executeAndLogAsync(source, new DeviceCommand(command.Name, value), cancellationToken);
    }

    private async Task runTextParameterCommandAsync(IDashboardSource source, CommandDescriptor command, CancellationToken cancellationToken)
    {
        TextPrompt<string> prompt = new TextPrompt<string>(Markup.Escape(command.Label + " (" + command.Unit + ", empty = cancel)"));
        prompt.AllowEmpty = true;
        string answer = await prompt.ShowAsync(_console, cancellationToken);
        if (answer.Trim().Length == 0)
        {
            return;
        }

        await executeAndLogAsync(source, new DeviceCommand(command.Name, answer.Trim()), cancellationToken);
    }

    private static ValidationResult validateParameter(string text, CommandDescriptor command, string range)
    {
        if (text.Trim().Length == 0)
        {
            return ValidationResult.Success(); // cancel
        }

        double value;
        if (!tryParseNumber(text, out value))
        {
            return ValidationResult.Error("Not a number");
        }

        if (value < command.Minimum || value > command.Maximum)
        {
            return ValidationResult.Error("Value must be between " + range);
        }

        return ValidationResult.Success();
    }

    private async Task executeAndLogAsync(IDashboardSource source, DeviceCommand command, CancellationToken cancellationToken)
    {
        try
        {
            CommandResult result = await source.executeAsync(command, cancellationToken);
            LogLevel level = LogLevel.Information;
            if (!result.Accepted)
            {
                level = LogLevel.Warning;
            }

            if (result.Message.Length > 0)
            {
                writeLog(level, result.Message); // empty = the device logs the outcome itself
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // shutdown
        }
        catch (Exception exception)
        {
            writeLog(LogLevel.Error, "Command " + command.Name + " failed: " + exception.Message); // surfaced in log panel
        }
    }

    private void writeLog(LogLevel level, string message)
    {
        _logBuffer.add(LogLineFormat.format(_timeProvider.GetUtcNow(), level, message));
    }

    private static bool tryParseNumber(string text, out double value)
    {
        string normalized = text.Trim().Replace(',', '.'); // decimal comma allowed
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static double parseNumber(string text)
    {
        double value;
        tryParseNumber(text, out value);
        return value;
    }

    private static string formatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
