using System.Globalization;
using EnergySim.Messaging;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Viewer;

/// <summary>Subscribes to the telemetry exchange and prints one block per message. Reconnects when the broker is lost.</summary>
public static class ViewerRunner
{
    private const int EXIT_OK = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_USAGE = 2;
    private const int BACKOFF_START_MS = 1000;
    private const int BACKOFF_MAX_MS = 10000;

    private const string USAGE = "usage: dotnet run --project src/EnergySim.Viewer -- [--Rabbit:Routing <pattern, default telemetry.#>] [--Rabbit:Host <host>] [--Rabbit:Port <port>]  (RABBITMQ_USER and RABBITMQ_PASSWORD from the environment)";

    public static async Task<int> runAsync(string[] args, CancellationTokenSource cancellation)
    {
        if (Array.IndexOf(args, "--help") >= 0)
        {
            await Console.Out.WriteLineAsync(USAGE);
            return EXIT_OK;
        }

        RabbitOptions options;
        string routing;
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true)
                .AddCommandLine(args)
                .Build();
            options = RabbitOptions.load(configuration);
            routing = readRouting(configuration);
        }
        catch (InvalidOperationException exception)
        {
            await Console.Error.WriteLineAsync("error: " + exception.Message);
            await Console.Error.WriteLineAsync(USAGE);
            return EXIT_USAGE;
        }

        try
        {
            Task quitWatcher = watchQuitAsync(cancellation);
            await receiveLoopAsync(options, routing, cancellation.Token);
            return EXIT_OK;
        }
        catch (OperationCanceledException)
        {
            await Console.Out.WriteLineAsync("viewer stopped");
            return EXIT_OK;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync("fatal: " + exception.Message);
            return EXIT_ERROR;
        }
    }

    private static string readRouting(IConfiguration configuration)
    {
        string? routing = configuration["Rabbit:Routing"];
        if (string.IsNullOrWhiteSpace(routing))
        {
            return Routing.BIND_ALL;
        }

        return routing.Trim();
    }

    private static async Task receiveLoopAsync(RabbitOptions options, string routing, CancellationToken cancellationToken)
    {
        await using TelemetryConsumer consumer = new TelemetryConsumer(options, routing, printMessageAsync, printInvalidAsync);
        int backoffMs = BACKOFF_START_MS;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.connectAsync(cancellationToken);
            }
            catch (BrokerUnavailableException exception)
            {
                await Console.Error.WriteLineAsync("broker: " + exception.Message + ", next try in " + backoffMs + " ms");
                await Task.Delay(backoffMs, cancellationToken);
                backoffMs = Math.Min(backoffMs * 2, BACKOFF_MAX_MS);
                continue;
            }

            backoffMs = BACKOFF_START_MS;
            await Console.Out.WriteLineAsync("viewer connected (" + options.Host + ":" + options.Port + "), routing " + routing + ", Ctrl+C or quit to stop");
            await consumer.Closed.WaitAsync(cancellationToken); // returns when the connection is lost
            await Console.Error.WriteLineAsync("broker connection lost, reconnecting");
        }
    }

    private static async Task printMessageAsync(TelemetryMessage message)
    {
        string simTime = "-";
        if (message.SimTimeUtc != null)
        {
            simTime = message.SimTimeUtc.Value.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        string mark = string.Empty;
        if (message.Quality == TelemetryQuality.Bad)
        {
            mark = "  !! BAD: " + message.Error;
        }

        if (message.Quality == TelemetryQuality.Uncertain)
        {
            mark = "  ! UNCERTAIN: " + string.Join(", ", message.WarnFlags) + " clock " + message.ClockState;
        }

        List<string> lines = new List<string>();
        lines.Add("[" + message.DeviceId + "] sim " + simTime + "  seq " + message.Sequence + "  " + message.Quality + "  " + message.State + mark);
        foreach (Measurement measurement in message.Measurements)
        {
            lines.Add("    " + measurement.Name + " = " + measurement.Value.ToString("0.##", CultureInfo.InvariantCulture) + " " + measurement.Unit);
        }

        await Console.Out.WriteLineAsync(string.Join(Environment.NewLine, lines));
    }

    private static async Task printInvalidAsync(string reason)
    {
        await Console.Error.WriteLineAsync("invalid message ignored: " + reason);
    }

    /// <summary>Typing quit on stdin stops the viewer like Ctrl+C. Closed stdin is ignored.</summary>
    private static async Task watchQuitAsync(CancellationTokenSource cancellation)
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                string? line = await Console.In.ReadLineAsync(cancellation.Token);
                if (line == null)
                {
                    return; // stdin closed
                }

                if (line.Trim().ToLowerInvariant() == "quit")
                {
                    cancellation.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // viewer already stopping
        }
    }
}
