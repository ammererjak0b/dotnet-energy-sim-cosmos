using System.Globalization;
using System.Net.Sockets;
using EnergySim.Modbus;
using EnergySim.Terminal;
using EnergySim.Time;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace EnergySim.Time.Cli;

/// <summary>Parses service options and composes authority, Modbus host, and terminal UI.</summary>
public static class TimeServiceRunner
{
    private const int LOG_LINE_CAPACITY = 200; // lines kept for dashboard and plain output
    private const int EXIT_PORT_IN_USE = 1;

    public static async Task<int> runAsync(string[] args, CancellationToken cancellationToken)
    {
        TimeServiceOptions options = parseOptions(args);
        options.validate();
        DateTimeOffset start = DateTimeOffset.UtcNow;
        if (options.InitialTimeUtc.Length > 0)
        {
            if (!DateTimeOffset.TryParse(options.InitialTimeUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out start))
            {
                throw new ArgumentException("--start must be a UTC ISO timestamp.");
            }
        }

        TimeProvider timeProvider = TimeProvider.System;
        TimeAuthority authority = new TimeAuthority(
            timeProvider,
            start,
            options.InitialSpeed,
            TimeSpan.FromMilliseconds(options.LeadTimeMilliseconds));
        ClockServiceHost host = new ClockServiceHost(authority, timeProvider, options);
        LogBuffer logBuffer = new LogBuffer(LOG_LINE_CAPACITY);
        using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        host.StatusChanged += delegate (string message)
        {
            logBuffer.add(LogLineFormat.format(timeProvider.GetUtcNow(), LogLevel.Information, message));
        };
        TimeServiceDashboardSource source = new TimeServiceDashboardSource(authority, host, start, logBuffer, shutdown.Cancel);
        Task? hostTask = null;
        Task? uiTask = null;
        try
        {
            if (!options.Headless && !TerminalMode.isInteractive())
            {
                throw new InvalidOperationException("Run the time service in an interactive terminal, or use --headless.");
            }

            hostTask = host.runAsync(shutdown.Token);
            if (options.Headless)
            {
                PlainConsoleWriter writer = new PlainConsoleWriter(Console.Out, timeProvider);
                TimeServiceHeadless headless = new TimeServiceHeadless(authority, host, source, timeProvider, logBuffer, writer);
                uiTask = headless.runAsync(new StreamReader(Console.OpenStandardInput()), shutdown.Token);
            }
            else
            {
                DeviceDashboard dashboard = new DeviceDashboard(AnsiConsole.Console, new ConsoleKeySource(), timeProvider, logBuffer);
                uiTask = dashboard.runAsync(source, shutdown.Token);
            }

            Task firstFinished = await Task.WhenAny(hostTask, uiTask);
            if (firstFinished == hostTask)
            {
                await hostTask; // surfaces a start failure such as port in use
            }

            await uiTask;
            return 0;
        }
        catch (Exception exception) when (isPortInUse(exception))
        {
            shutdown.Cancel(); // release the terminal before writing to stderr
            if (uiTask != null)
            {
                await Task.WhenAny(uiTask);
            }

            await Console.Error.WriteLineAsync("error: port " + options.Port + " is already in use. Is another time service running? Choose another --port.");
            return EXIT_PORT_IN_USE;
        }
        finally
        {
            shutdown.Cancel();
            if (uiTask != null)
            {
                await Task.WhenAny(uiTask); // UI releases the terminal first
            }

            if (hostTask != null)
            {
                await Task.WhenAny(hostTask); // waits without throwing
                await reportLateFaultAsync(hostTask);
            }

            host.Dispose();
        }
    }

    private static bool isPortInUse(Exception exception)
    {
        return exception is SocketException || exception is DeviceModbusPortInUseException;
    }

    /// <summary>A host fault nobody awaited (UI ended first) must not vanish; port-in-use was reported already.</summary>
    private static async Task reportLateFaultAsync(Task hostTask)
    {
        if (!hostTask.IsFaulted || hostTask.Exception == null)
        {
            return;
        }

        Exception fault = hostTask.Exception.GetBaseException();
        if (!isPortInUse(fault))
        {
            await Console.Error.WriteLineAsync("error: " + fault.Message);
        }
    }

    private static TimeServiceOptions parseOptions(string[] args)
    {
        TimeServiceOptions options = new TimeServiceOptions();
        for (int index = 0; index < args.Length; index++)
        {
            string key = args[index];
            if (key == "--headless")
            {
                options.Headless = true;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Missing value for " + key + ".");
            }

            string value = args[index + 1];
            index++;
            if (key == "--port")
            {
                options.Port = parseInteger(key, value);
            }
            else if (key == "--lead-ms")
            {
                options.LeadTimeMilliseconds = parseInteger(key, value);
            }
            else if (key == "--heartbeat-ms")
            {
                options.HeartbeatMilliseconds = parseInteger(key, value);
            }
            else if (key == "--heartbeat-expiry")
            {
                options.HeartbeatExpirySeconds = parseInteger(key, value);
            }
            else if (key == "--start")
            {
                options.InitialTimeUtc = value;
            }
            else if (key == "--speed")
            {
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed))
                {
                    throw new ArgumentException("--speed must be a number.");
                }

                options.InitialSpeed = speed;
            }
            else
            {
                throw new ArgumentException("Unknown option " + key + ".");
            }
        }

        return options;
    }

    private static int parseInteger(string option, string value)
    {
        int result;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            throw new ArgumentException(option + " must be an integer.");
        }

        return result;
    }
}
