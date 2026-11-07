using EnergySim.Terminal;
using EnergySim.Weather;
using System.Net.Sockets;
using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.History;
using EnergySim.Core.Weather;
using EnergySim.Modbus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.Globalization;

namespace EnergySim.Cli;

/// <summary>Wires everything: settings, device, clock, loop, sinks, then dashboard or headless output.</summary>
public static class CliRunner
{
    private const int EXIT_OK = 0;
    private const int EXIT_USAGE = 2;
    private const int EXIT_PORT_IN_USE = 1;
    private const int LOG_BUFFER_LINES = 200;

    private static readonly string USAGE = "usage: dotnet run --project src/EnergySim.Cli -- --device " + string.Join("|", CliSettings.DEVICE_NAMES) + " [--id <id>] [--clock local|shared] [--clock-port <port>] [--time-port <port>] [--start <UTC ISO>] [--speed <factor>] [--location salzburg|wien|innsbruck] [--weather-url <http url>] [--csv <path>] [--Modbus:Host <ip>] [--Modbus:Port <port>] [--Modbus:UnitId <" + PortRules.MIN_UNIT_ID + ".." + PortRules.MAX_UNIT_ID + ">]";

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
        catch (SocketException exception) // port in use (also DeviceModbusPortInUseException), bind errors
        {
            await Console.Error.WriteLineAsync("error: " + exception.Message);
            return EXIT_PORT_IN_USE;
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
            weatherClient = await createWeatherClientAsync(settings, cancellationToken);
        }

        await runSimulationAsync(settings, device, weatherClient, isInteractive, cancellationToken);
    }

    /// <summary>Weather process is required: one probe request at start, no local fallback.</summary>
    private static async Task<IWeatherClient> createWeatherClientAsync(CliSettings settings, CancellationToken cancellationToken)
    {
        HttpWeatherClient client = new HttpWeatherClient(new HttpClient(), settings.WeatherUrl);
        WeatherResult probe = await client.getWeatherAsync(settings.Location, settings.Start, cancellationToken);
        if (probe.Weather == null)
        {
            throw new WeatherDataException("weather process not reachable at " + settings.WeatherUrl + " (" + probe.ErrorMessage + "). Start it first: dotnet run --project src/EnergySim.WeatherService");
        }

        return client;
    }

    private static async Task runSimulationAsync(CliSettings settings, ISimulatedDevice device, IWeatherClient? weatherClient, bool isInteractive, CancellationToken cancellationToken)
    {
        TimeProvider timeProvider = TimeProvider.System;
        LogBuffer logBuffer = new LogBuffer(LOG_BUFFER_LINES);
        LocalClock? localClock = null;
        SharedClockConnection? sharedClock = null;
        IClockSource clockSource;
        if (settings.ClockMode == "shared")
        {
            SharedClockConnectionOptions connectionOptions = new SharedClockConnectionOptions();
            connectionOptions.ServiceHost = settings.TimeHost;
            connectionOptions.ServicePort = settings.TimePort;
            ClockEndpointOptions endpointOptions = new ClockEndpointOptions();
            endpointOptions.Port = settings.ClockPort;
            endpointOptions.DeviceId = settings.Id;
            sharedClock = new SharedClockConnection(connectionOptions, endpointOptions, timeProvider, delegate (string message)
            {
                logBuffer.add(LogLineFormat.format(timeProvider.GetUtcNow(), LogLevel.Information, message));
            });
            clockSource = sharedClock.Clock;
        }
        else
        {
            localClock = new LocalClock(timeProvider, settings.Start, settings.Speed);
            localClock.pause(); // start time must not run away while the loop starts
            clockSource = localClock;
        }
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

        ClockDashboardSource source = new ClockDashboardSource(device, locationId, clockSource, localClock, commands.Writer, logBuffer, shutdown.Cancel);
        List<ISimulationSink> sinks = new List<ISimulationSink>();
        sinks.Add(source);
        CsvHistoryWriter? csvWriter = null;
        string csvPath = settings.CsvPath;
        bool hasDeviceCsvFields = device.CsvHistoryFields.Count > 0;
        if (hasDeviceCsvFields && csvPath.Length == 0)
        {
            string exportDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data", "export");
            Directory.CreateDirectory(exportDirectory);
            string date = timeProvider.GetLocalNow().ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            string simulatorName = device.Type.ToString().ToLowerInvariant();
            csvPath = Path.Combine(exportDirectory, simulatorName + "_" + date + ".csv");
        }

        if (csvPath.Length > 0)
        {
            if (hasDeviceCsvFields)
            {
                csvWriter = new CsvHistoryWriter(csvPath, timeProvider);
            }
            else
            {
                csvWriter = new CsvHistoryWriter(csvPath, timeProvider, CsvHistoryWriter.DEFAULT_INTERVAL);
            }

            sinks.Add(csvWriter);
        }

        DeviceModbusOptions modbusOptions = new DeviceModbusOptions();
        modbusOptions.Host = settings.ModbusHost;
        modbusOptions.Port = settings.ModbusPort;
        modbusOptions.UnitId = settings.ModbusUnitId;
        modbusOptions.DeviceType = device.Type;
        using DeviceModbusServer modbusServer = new DeviceModbusServer(modbusOptions, commands.Writer);
        sinks.Add(new DeviceModbusSink(modbusServer));
        SimulationOptions options = new SimulationOptions();
        options.LocationId = locationId;
        DeviceSimulationLoop loop = new DeviceSimulationLoop(device, clockSource, weatherClient, new FanOutSink(sinks), commands.Reader, options, timeProvider, loggerFactory.CreateLogger<DeviceSimulationLoop>());
        await modbusServer.startAsync(shutdown.Token); // port in use throws here
        Task? sharedClockTask = null;
        if (sharedClock != null)
        {
            sharedClockTask = sharedClock.runAsync(shutdown.Token);
        }

        Task loopTask = loop.runAsync(shutdown.Token);
        try
        {
            Task firstFrameTask = source.waitForFirstFrameAsync(shutdown.Token);
            if (sharedClockTask != null)
            {
                Task finishedTask = await Task.WhenAny(firstFrameTask, sharedClockTask);
                if (finishedTask == sharedClockTask)
                {
                    await sharedClockTask; // clock start failed (port in use): rethrow
                }
            }

            await firstFrameTask;
            if (localClock != null)
            {
                localClock.resume();
            }

            await runUserInterfaceAsync(isInteractive, source, logBuffer, timeProvider, shutdown);
        }
        finally
        {
            shutdown.Cancel();
            await shutDownAsync(loopTask, modbusServer, sharedClockTask, csvWriter);
        }
    }

    /// <summary>Every cleanup step runs, even when an earlier one throws.</summary>
    private static async Task shutDownAsync(Task loopTask, DeviceModbusServer modbusServer, Task? sharedClockTask, CsvHistoryWriter? csvWriter)
    {
        try
        {
            await loopTask;
        }
        finally
        {
            modbusServer.stop();
            try
            {
                if (sharedClockTask != null)
                {
                    await sharedClockTask;
                }
            }
            finally
            {
                if (csvWriter != null)
                {
                    await csvWriter.DisposeAsync();
                }
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
