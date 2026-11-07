using System.Globalization;
using System.Threading.Channels;
using EnergySim.Core.Engine;
using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using Microsoft.Extensions.Logging;

namespace EnergySim.Core.Engine;

/// <summary>
/// Generic tick loop for any ISimulatedDevice. Each tick: clock snapshot => reset on new generation
/// => commands => step = difference of two clock readings (never own integration) => weather => update => publish.
/// No console, HTTP or configuration in here.
/// </summary>
public sealed class DeviceSimulationLoop
{
    public const int ERROR_LOG_INTERVAL_SECONDS = 10; // repeated device/sink errors: one log line per interval

    private readonly ISimulatedDevice _device;

    private readonly IClockSource _clock;

    private readonly IWeatherClient? _weatherClient;

    private readonly ISimulationSink _sink;

    private readonly ChannelReader<DeviceCommand> _commands;

    private readonly SimulationOptions _options;

    private readonly TimeProvider _timeProvider;

    private readonly ILogger<DeviceSimulationLoop> _logger;

    private bool _hasSynced; // false until the first synced tick (device then reset to virtual time)
    private readonly ClockCursor _clockCursor = new ClockCursor();

    private int _lastGeneration;

    private Guid _lastSessionId;

    private ClockStatus _lastStatus = ClockStatus.Stopped;

    private DateTimeOffset _lastVirtualNow;

    private bool _needsInitialUpdate; // after sync/reset: one update with zero step so the device has weather

    private uint _sequence;

    private bool _weatherStale;

    private string _weatherInfo = string.Empty;

    private string _lastCommandMessage = string.Empty;

    private string _deviceError = string.Empty;

    private DateTimeOffset _lastDeviceErrorLogUtc = DateTimeOffset.MinValue;

    private DateTimeOffset _lastSinkErrorLogUtc = DateTimeOffset.MinValue;

    /// <summary>weatherClient may be null only when the device does not need weather.</summary>
    public DeviceSimulationLoop(
        ISimulatedDevice device,
        IClockSource clock,
        IWeatherClient? weatherClient,
        ISimulationSink sink,
        ChannelReader<DeviceCommand> commands,
        SimulationOptions options,
        TimeProvider timeProvider,
        ILogger<DeviceSimulationLoop> logger)
    {
        if (device == null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (device.NeedsWeather && weatherClient == null)
        {
            throw new ArgumentNullException(nameof(weatherClient), "device needs weather");
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        validateOptions(options, device.NeedsWeather);

        _device = device;
        _clock = clock;
        _weatherClient = weatherClient;
        _sink = sink;
        _commands = commands;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Runs until ct is cancelled, then returns normally. First frame is published immediately.</summary>
    public async Task runAsync(CancellationToken ct)
    {
        TimeSpan period = TimeSpan.FromMilliseconds(_options.TickMilliseconds);
        using (PeriodicTimer timer = new PeriodicTimer(period, _timeProvider))
        {
            _logger.LogInformation("Simulation loop started for {DeviceId}, tick {TickMilliseconds} ms", _device.DeviceId, _options.TickMilliseconds);
            try
            {
                await runTickAsync(ct); // registers readable from the start
                while (await timer.WaitForNextTickAsync(ct))
                {
                    await runTickAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Simulation loop stopped for {DeviceId}", _device.DeviceId);
            }
        }
    }

    private static void validateOptions(SimulationOptions options, bool needsWeather)
    {
        if (options.TickMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "TickMilliseconds must be positive");
        }

        if (options.MaxStepSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxStepSeconds must be positive");
        }

        if (needsWeather && string.IsNullOrWhiteSpace(options.LocationId))
        {
            throw new ArgumentException("LocationId required when the device needs weather", nameof(options));
        }
    }

    private async Task runTickAsync(CancellationToken ct)
    {
        IReadOnlyList<ClockSnapshot> snapshots = _clock.readAdvance(_clockCursor);
        foreach (ClockSnapshot snapshot in snapshots)
        {
            if (snapshot.IsSynced || snapshot.IsUsingFallback)
            {
                try
                {
                    await advanceDeviceAsync(snapshot, ct);
                    clearDeviceError();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    reportDeviceError(exception);
                }
            }
        }

        await publishAsync(snapshots[snapshots.Count - 1], ct);
    }

    private async Task advanceDeviceAsync(ClockSnapshot snapshot, CancellationToken ct)
    {
        bool isNewGeneration = !_hasSynced || snapshot.SessionId != _lastSessionId || snapshot.Generation != _lastGeneration;
        bool isClockStart = _hasSynced && _lastStatus == ClockStatus.Stopped && snapshot.Status == ClockStatus.Running; // clock start: time jumps from epoch to start time
        if (isNewGeneration || isClockStart)
        {
            resetDevice(snapshot);
        }

        _lastStatus = snapshot.Status;

        applyPendingCommands();

        TimeSpan step = calculateStep(snapshot);
        if (step > TimeSpan.Zero || _needsInitialUpdate)
        {
            WeatherDto? weather = await fetchWeatherAsync(snapshot.VirtualNow, ct);
            _device.update(snapshot.VirtualNow, step, weather);
            _needsInitialUpdate = false;
            _sequence++;
        }
    }

    /// <summary>First sync or master restart: device back to initial state at the new virtual time, no step.</summary>
    private void resetDevice(ClockSnapshot snapshot)
    {
        _device.reset(snapshot.VirtualNow);
        if (_hasSynced && snapshot.SessionId != _lastSessionId)
        {
            _logger.LogInformation("New time service session: device reset at {VirtualNow:o}", snapshot.VirtualNow);
        }
        else if (_hasSynced && snapshot.Generation == _lastGeneration)
        {
            _logger.LogInformation("Clock start: device reset at {VirtualNow:o}", snapshot.VirtualNow);
        }
        else if (_hasSynced)
        {
            _logger.LogInformation("Clock restart: generation {OldGeneration} -> {NewGeneration}, device reset at {VirtualNow:o}", _lastGeneration, snapshot.Generation, snapshot.VirtualNow);
        }
        else
        {
            _logger.LogInformation("Clock synced: generation {Generation}, device reset at {VirtualNow:o}", snapshot.Generation, snapshot.VirtualNow);
        }

        _hasSynced = true;
        _lastGeneration = snapshot.Generation;
        _lastSessionId = snapshot.SessionId;
        _lastVirtualNow = snapshot.VirtualNow;
        _needsInitialUpdate = true;
        _sequence++;
    }

    private void applyPendingCommands()
    {
        while (_commands.TryRead(out DeviceCommand? command))
        {
            CommandResult result = _device.applyCommand(command);
            _lastCommandMessage = result.Message;
            if (result.Accepted)
            {
                _sequence++;
                _logger.LogInformation("Command {Command} {Value} applied: {Message}", command.Name, command.Value, result.Message);
            }
            else
            {
                _logger.LogWarning("Command {Command} {Value} rejected: {Message}", command.Name, command.Value, result.Message);
            }
        }
    }

    /// <summary>Invariant: step = difference of two clock readings, only while running. Never t += dt * speed.</summary>
    private TimeSpan calculateStep(ClockSnapshot snapshot)
    {
        TimeSpan step = TimeSpan.Zero;
        if (snapshot.Status == ClockStatus.Running)
        {
            step = snapshot.VirtualNow - _lastVirtualNow;
        }

        _lastVirtualNow = snapshot.VirtualNow; // always, so pause/resume never jumps

        if (step < TimeSpan.Zero)
        {
            step = TimeSpan.Zero; // backward reading without restart
        }

        TimeSpan maxStep = TimeSpan.FromSeconds(_options.MaxStepSeconds);
        if (step > maxStep)
        {
            _logger.LogWarning("Step {Step} larger than {MaxStep}, clamped", step, maxStep);
            step = maxStep;
        }

        return step;
    }

    private async Task<WeatherDto?> fetchWeatherAsync(DateTimeOffset virtualNow, CancellationToken ct)
    {
        if (!_device.NeedsWeather || _weatherClient == null)
        {
            return null;
        }

        WeatherResult result = await _weatherClient.getWeatherAsync(_options.LocationId, virtualNow, ct); // client quantizes to the minute
        _weatherStale = result.IsStale || result.Weather == null; // stale value or none at all
        _weatherInfo = formatWeather(result);
        return result.Weather; // null => device decides (e.g. P = 0 + fault code)
    }

    private static string formatWeather(WeatherResult result)
    {
        if (result.Weather == null)
        {
            return "no weather: " + result.ErrorMessage;
        }

        WeatherDto weather = result.Weather;
        string text = string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.0} degC, cloud {1:0} %, wind {2:0.0} m/s",
            weather.TemperatureC,
            weather.CloudCoverPct,
            weather.WindSpeedMs);
        if (result.IsStale)
        {
            text = text + " (stale: " + result.ErrorMessage + ")";
        }

        return text;
    }

    private void reportDeviceError(Exception exception)
    {
        string error = "device error: " + exception.Message;
        bool isNewError = error != _deviceError;
        _deviceError = error;
        if (isNewError || isLogIntervalOver(_lastDeviceErrorLogUtc))
        {
            _logger.LogError(exception, "Device {DeviceId} failed, loop keeps running", _device.DeviceId);
            _lastDeviceErrorLogUtc = _timeProvider.GetUtcNow();
        }
    }

    private void clearDeviceError()
    {
        if (_deviceError.Length > 0)
        {
            _logger.LogInformation("Device {DeviceId} recovered", _device.DeviceId);
            _deviceError = string.Empty;
        }
    }

    private bool isLogIntervalOver(DateTimeOffset lastLogUtc)
    {
        return _timeProvider.GetUtcNow() - lastLogUtc >= TimeSpan.FromSeconds(ERROR_LOG_INTERVAL_SECONDS);
    }

    private async Task publishAsync(ClockSnapshot snapshot, CancellationToken ct)
    {
        SimulationFrame frame = buildFrame(snapshot);
        try
        {
            await _sink.publishAsync(frame, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (isLogIntervalOver(_lastSinkErrorLogUtc))
            {
                _logger.LogError(exception, "Publishing frame failed, loop keeps running");
                _lastSinkErrorLogUtc = _timeProvider.GetUtcNow();
            }
        }
    }

    private SimulationFrame buildFrame(ClockSnapshot snapshot)
    {
        SimulationFrame frame = new SimulationFrame();
        frame.VirtualNow = snapshot.VirtualNow;
        frame.Snapshot = snapshot;
        frame.Header = copyHeader(_device.Header);
        frame.DisplayRows = new List<DisplayRow>(_device.DisplayRows);
        frame.CsvHistoryFields = new List<EnergySim.Core.History.CsvHistoryField>(_device.CsvHistoryFields);
        frame.Sequence = _sequence;
        frame.WeatherStale = _weatherStale;
        frame.WeatherInfo = _weatherInfo;
        frame.LastCommandMessage = _lastCommandMessage;
        frame.DeviceError = _deviceError;
        return frame;
    }

    private static DeviceHeader copyHeader(DeviceHeader source)
    {
        DeviceHeader copy = new DeviceHeader();
        copy.State = source.State;
        copy.FaultCode = source.FaultCode;
        copy.ActivePowerW = source.ActivePowerW;
        copy.EnergyWh = source.EnergyWh;
        copy.IsOn = source.IsOn;
        copy.PowerLimitPercent = source.PowerLimitPercent;
        return copy;
    }
}
