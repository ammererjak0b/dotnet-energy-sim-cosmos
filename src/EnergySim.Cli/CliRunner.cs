using EnergySim.Terminal;
using EnergySim.Weather;
using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.History;
using EnergySim.Core.Weather;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace EnergySim.Cli;

/// <summary>Wires everything: settings, device, clock, loop, sinks, then dashboard or headless output.</summary>
public static class CliRunner
{
    private const int EXIT_OK = 0;
    private const int EXIT_USAGE = 2;
    private const int LOG_BUFFER_LINES = 200;

    private const string USAGE = "usage: dotnet run --project src/EnergySim.Cli -- --device pv|battery|charger [--id <id>] [--start <UTC ISO>] [--speed <factor>] [--location salzburg|wien|innsbruck] [--csv <path>]";

    public static async Task<int> runAsync(string[] args, CancellationToken cancellationToken)
    {
        try
        {
            if (Array.IndexOf(args, "--help") >= 0)
            {
                await Console.Out.WriteLineAsync(USAGE);
                return EXIT_OK;
            }

            await runCoreAsync(args, cancellationToken);
            return EXIT_OK;
        }
        catch (CliUsageException exception)
        {
            await Console.Error.WriteLineAsync("error: " + exception.Message);
            await Console.Error.WriteLineAsync(USAGE);
            return EXIT_USAGE;
        }
        catch (WeatherDataException exception)
        {
            await Console.Error.WriteLineAsync("error: " + exception.Message);
            return EXIT_USAGE;
        }
        catch (OperationCanceledException)
        {
            return EXIT_OK; // Ctrl+C before start finished
        }
    }

    private static async Task runCoreAsync(string[] args, CancellationToken cancellationToken)
    {
        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.SetBasePath(AppContext.BaseDirectory);
        builder.AddJsonFile("appsettings.json", true);
        builder.AddCommandLine(args);
        IConfiguration configuration = builder.Build();

        bool isInteractive = TerminalMode.isInteractive();
        CliSettings settings = CliSettings.load(configuration);
        if (settings.Device.Length == 0)
        {
            if (!isInteractive)
            {
                throw new CliUsageException("device: missing");
            }

            settings = settings.withDevice(await DevicePicker.pickAsync(AnsiConsole.Console, cancellationToken));
        }

        ISimulatedDevice device = DeviceFactory.create(settings, configuration);
        IWeatherClient? weatherClient = null;
        if (device.NeedsWeather)
        {
            weatherClient = await createWeatherClientAsync(cancellationToken);
        }

        await runSimulationAsync(settings, device, weatherClient, isInteractive, cancellationToken);
    }

    private static async Task<IWeatherClient> createWeatherClientAsync(CancellationToken cancellationToken)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "data");
        IReadOnlyDictionary<string, WeatherDataSet> dataSets = await WeatherDataLoader.loadDirectoryAsync(directory, cancellationToken);
        return new LocalWeatherClient(new WeatherModel(dataSets));
    }

    private static async Task runSimulationAsync(CliSettings settings, ISimulatedDevice device, IWeatherClient? weatherClient, bool isInteractive, CancellationToken cancellationToken)
    {
        TimeProvider timeProvider = TimeProvider.System;
        LocalClock clock = new LocalClock(timeProvider, settings.Start, settings.Speed);
        clock.pause(); // start time must not run away while the loop starts
        LogBuffer logBuffer = new LogBuffer(LOG_BUFFER_LINES);
        using ILoggerFactory loggerFactory = LoggerFactory.Create(delegate (ILoggingBuilder logging)
        {
            logging.AddProvider(new DashboardLoggerProvider(logBuffer, timeProvider)); // only the UI writes to the console
        });
        using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Channel<DeviceCommand> commands = Channel.CreateUnbounded<DeviceCommand>();
        string locationId = string.Empty;
        if (device.NeedsWeather)
        {
            locationId = settings.Location;
        }

        ClockDashboardSource source = new ClockDashboardSource(device, locationId, clock, commands.Writer, logBuffer, shutdown.Cancel);
        List<ISimulationSink> sinks = new List<ISimulationSink>();
        sinks.Add(source);
        CsvHistoryWriter? csvWriter = null;
        if (settings.CsvPath.Length > 0)
        {
            csvWriter = new CsvHistoryWriter(settings.CsvPath, timeProvider, CsvHistoryWriter.DEFAULT_INTERVAL);
            sinks.Add(csvWriter);
        }

        SimulationOptions options = new SimulationOptions();
        options.LocationId = locationId;
        DeviceSimulationLoop loop = new DeviceSimulationLoop(device, clock, weatherClient, new FanOutSink(sinks), commands.Reader, options, timeProvider, loggerFactory.CreateLogger<DeviceSimulationLoop>());
        Task loopTask = loop.runAsync(shutdown.Token);
        try
        {
            await source.waitForFirstFrameAsync(shutdown.Token);
            clock.resume();
            await runUserInterfaceAsync(isInteractive, source, logBuffer, timeProvider, shutdown);
        }
        finally
        {
            shutdown.Cancel();
            await loopTask;
            if (csvWriter != null)
            {
                await csvWriter.DisposeAsync();
            }
        }
    }

    private static async Task runUserInterfaceAsync(bool isInteractive, ClockDashboardSource source, LogBuffer logBuffer, TimeProvider timeProvider, CancellationTokenSource shutdown)
    {
        if (isInteractive)
        {
            DeviceDashboard dashboard = new DeviceDashboard(AnsiConsole.Console, new ConsoleKeySource(), timeProvider, logBuffer);
            await dashboard.runAsync(source, shutdown.Token);
            return;
        }

        await HeadlessRunner.runAsync(source, logBuffer, Console.Out, Console.In, timeProvider, shutdown);
    }
}
