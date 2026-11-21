using EnergySim.Messaging;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Gateway;

/// <summary>Poll loop: reads every device over Modbus, publishes new states to RabbitMQ, reports errors on the console.</summary>
public static class GatewayRunner
{
    private const int EXIT_OK = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_USAGE = 2;
    private const int BACKOFF_START_MS = 1000;
    private const int BACKOFF_MAX_MS = 10000;

    private const string USAGE = "usage: dotnet run --project src/EnergySim.Gateway -- [--PollIntervalMs <100..60000>] [--UnitId <1..247>] [--Rabbit:Host <host>] [--Rabbit:Port <port>]  (RABBITMQ_USER and RABBITMQ_PASSWORD from the environment)";

    public static async Task<int> runAsync(string[] args, CancellationTokenSource cancellation)
    {
        if (Array.IndexOf(args, "--help") >= 0)
        {
            await Console.Out.WriteLineAsync(USAGE);
            return EXIT_OK;
        }

        GatewaySettings settings;
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true)
                .AddCommandLine(args)
                .Build();
            settings = GatewaySettings.load(configuration);
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
            await pollLoopAsync(settings, cancellation.Token);
            return EXIT_OK;
        }
        catch (OperationCanceledException)
        {
            await Console.Out.WriteLineAsync("gateway stopped");
            return EXIT_OK;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync("fatal: " + exception.Message);
            return EXIT_ERROR;
        }
    }

    private static async Task pollLoopAsync(GatewaySettings settings, CancellationToken cancellationToken)
    {
        List<GatewayDevice> devices = new List<GatewayDevice>();
        foreach (GatewayDeviceConfig config in settings.Devices)
        {
            devices.Add(new GatewayDevice(config, settings.UnitId));
        }

        await using TelemetryPublisher publisher = new TelemetryPublisher(settings.Rabbit);
        int backoffMs = BACKOFF_START_MS;
        DateTime nextConnectAttemptUtc = DateTime.UtcNow;
        long droppedCount = 0;
        await Console.Out.WriteLineAsync("gateway started, " + devices.Count + " devices, every " + settings.PollIntervalMs + " ms, Ctrl+C or quit to stop");

        try
        {
            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.PollIntervalMs));
            do
            {
                if (!publisher.IsOpen && DateTime.UtcNow >= nextConnectAttemptUtc)
                {
                    try
                    {
                        await publisher.connectAsync(cancellationToken);
                        await Console.Out.WriteLineAsync("broker connected (" + settings.Rabbit.Host + ":" + settings.Rabbit.Port + "), dropped so far: " + droppedCount);
                        backoffMs = BACKOFF_START_MS;
                        foreach (GatewayDevice device in devices)
                        {
                            device.forgetPublished(); // latest state goes out first
                        }
                    }
                    catch (BrokerUnavailableException exception)
                    {
                        await Console.Error.WriteLineAsync("broker: " + exception.Message + ", next try in " + backoffMs + " ms");
                        nextConnectAttemptUtc = DateTime.UtcNow.AddMilliseconds(backoffMs);
                        backoffMs = Math.Min(backoffMs * 2, BACKOFF_MAX_MS);
                    }
                }

                foreach (GatewayDevice device in devices)
                {
                    TelemetryMessage? message = device.poll(DateTime.UtcNow, cancellationToken);
                    if (message == null)
                    {
                        continue;
                    }

                    bool published = await tryPublishAsync(publisher, message, cancellationToken);
                    if (!published)
                    {
                        droppedCount++;
                    }
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        finally
        {
            foreach (GatewayDevice device in devices)
            {
                device.Dispose();
            }
        }
    }

    private static async Task<bool> tryPublishAsync(TelemetryPublisher publisher, TelemetryMessage message, CancellationToken cancellationToken)
    {
        if (message.Quality == TelemetryQuality.Bad)
        {
            await Console.Error.WriteLineAsync("device " + message.DeviceId + ": " + message.Error);
        }

        if (!publisher.IsOpen)
        {
            await Console.Error.WriteLineAsync("dropped " + message.DeviceId + " seq " + message.Sequence + " (broker down)");
            return false;
        }

        try
        {
            await publisher.publishAsync(message, cancellationToken);
        }
        catch (BrokerUnavailableException exception)
        {
            await Console.Error.WriteLineAsync("broker: " + exception.Message + ", dropped " + message.DeviceId + " seq " + message.Sequence);
            return false;
        }

        await Console.Out.WriteLineAsync(message.DeviceId + " seq " + message.Sequence + " " + message.Quality + " published");
        return true;
    }

    /// <summary>Typing quit on stdin stops the gateway like Ctrl+C. Closed stdin is ignored.</summary>
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
            // gateway already stopping
        }
    }
}
