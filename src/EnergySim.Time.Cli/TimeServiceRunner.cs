using System.Globalization;
using EnergySim.Modbus;
using EnergySim.Terminal;
using EnergySim.Time;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace EnergySim.Time.Cli;

/// <summary>Parses service options and composes authority, Modbus host, and terminal UI.</summary>
public static class TimeServiceRunner
{
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
        LogBuffer logBuffer = new LogBuffer(200);
        using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        host.StatusChanged += delegate (string message)
        {
            logBuffer.add(LogLineFormat.format(timeProvider.GetUtcNow(), LogLevel.Information, message));
        };
        Task? hostTask = null;
        try
        {
            if (!TerminalMode.isInteractive())
            {
                throw new InvalidOperationException("Run the time service in an interactive terminal.");
            }

            hostTask = host.runAsync(shutdown.Token);
            TimeServiceDashboardSource source = new TimeServiceDashboardSource(authority, host, start, logBuffer, shutdown.Cancel);
            DeviceDashboard dashboard = new DeviceDashboard(AnsiConsole.Console, new ConsoleKeySource(), timeProvider, logBuffer);
            await dashboard.runAsync(source, shutdown.Token);
            return 0;
        }
        finally
        {
            shutdown.Cancel();
            if (hostTask != null)
            {
                await hostTask;
            }

            host.Dispose();
        }
    }

    private static TimeServiceOptions parseOptions(string[] args)
    {
        TimeServiceOptions options = new TimeServiceOptions();
        for (int index = 0; index < args.Length; index++)
        {
            string key = args[index];
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
