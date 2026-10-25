using EnergySim.Weather;
using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.Weather;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Cli;

/// <summary>Command line options include local/shared clock connection settings.</summary>
public sealed class CliSettings
{
    public const string DEFAULT_START = "2026-06-21T04:00:00Z";
    public const double DEFAULT_SPEED = 60.0;
    public const string DEFAULT_LOCATION = "salzburg";

    public static readonly IReadOnlyList<string> DEVICE_NAMES = new List<string> { "pv", "battery", "charger" };

    public string Device { get; private set; } = string.Empty; // empty = ask in the picker

    public string Id { get; private set; } = string.Empty;

    public DateTimeOffset Start { get; private set; }

    public double Speed { get; private set; }

    public string Location { get; private set; } = DEFAULT_LOCATION;

    public string CsvPath { get; private set; } = string.Empty; // empty = no CSV

    public string ClockMode { get; private set; } = "local";

    public string TimeHost { get; private set; } = "127.0.0.1";

    public int TimePort { get; private set; } = 1502;

    public int ClockPort { get; private set; }

    /// <summary>Throws CliUsageException with the option name for invalid values.</summary>
    public static CliSettings load(IConfiguration configuration)
    {
        CliSettings settings = new CliSettings();
        settings.Device = readText(configuration, "device", string.Empty).ToLowerInvariant();
        if (settings.Device.Length > 0 && !DEVICE_NAMES.Contains(settings.Device))
        {
            throw new CliUsageException("device: unknown '" + settings.Device + "', use " + string.Join("|", DEVICE_NAMES));
        }

        settings.Id = readText(configuration, "id", string.Empty);
        settings.Start = readStart(configuration);
        settings.Speed = readSpeed(configuration);
        settings.Location = readText(configuration, "location", DEFAULT_LOCATION).ToLowerInvariant();
        if (!LocationCatalog.ids().Contains(settings.Location))
        {
            throw new CliUsageException("location: unknown '" + settings.Location + "', use " + string.Join("|", LocationCatalog.ids()));
        }

        settings.CsvPath = readText(configuration, "csv", string.Empty);
        settings.ClockMode = readText(configuration, "clock", "local").ToLowerInvariant();
        if (settings.ClockMode != "local" && settings.ClockMode != "shared")
        {
            throw new CliUsageException("clock: use local or shared");
        }

        settings.TimeHost = readText(configuration, "time-host", "127.0.0.1");
        if (settings.TimeHost != "localhost" && settings.TimeHost != "127.0.0.1")
        {
            throw new CliUsageException("time-host: shared clock must use localhost");
        }

        settings.TimePort = readPort(configuration, "time-port", 1502);
        settings.ClockPort = readPort(configuration, "clock-port", 0);
        settings.applyDeviceChoice(settings.Device);
        if (settings.ClockPort == 0 && settings.Device.Length > 0)
        {
            settings.ClockPort = defaultClockPort(settings.Device);
        }

        return settings;
    }

    /// <summary>Picker result: sets the device and the default id when no --id was given.</summary>
    public CliSettings withDevice(string device)
    {
        CliSettings copy = (CliSettings)MemberwiseClone();
        copy.Device = device;
        copy.Id = string.Empty;
        copy.applyDeviceChoice(device);
        if (copy.ClockPort == 0)
        {
            copy.ClockPort = defaultClockPort(device);
        }

        return copy;
    }

    private void applyDeviceChoice(string device)
    {
        if (Id.Length == 0 && device.Length > 0)
        {
            Id = device + "-1";
        }
    }

    private static string readText(IConfiguration configuration, string key, string fallback)
    {
        string? value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim();
    }

    private static DateTimeOffset readStart(IConfiguration configuration)
    {
        string text = readText(configuration, "start", DEFAULT_START);
        DateTimeOffset start;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out start))
        {
            throw new CliUsageException("start: '" + text + "' is not a UTC ISO time, e.g. " + DEFAULT_START);
        }

        return start;
    }

    private static double readSpeed(IConfiguration configuration)
    {
        string text = readText(configuration, "speed", DEFAULT_SPEED.ToString(CultureInfo.InvariantCulture));
        double speed;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out speed))
        {
            throw new CliUsageException("speed: '" + text + "' is not a number");
        }

        if (speed < LocalClock.MIN_SPEED || speed > LocalClock.MAX_SPEED)
        {
            throw new CliUsageException("speed: must be between " + LocalClock.MIN_SPEED + " and " + LocalClock.MAX_SPEED);
        }

        return speed;
    }

    private static int readPort(IConfiguration configuration, string key, int fallback)
    {
        string text = readText(configuration, key, fallback.ToString(CultureInfo.InvariantCulture));
        int port;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
        {
            throw new CliUsageException(key + ": port must be between 1024 and 65535");
        }

        if (port == 0 && fallback == 0)
        {
            return 0;
        }

        if (port < 1024 || port > 65535)
        {
            throw new CliUsageException(key + ": port must be between 1024 and 65535");
        }

        return port;
    }

    private static int defaultClockPort(string device)
    {
        if (device == "pv")
        {
            return 1601;
        }

        if (device == "battery")
        {
            return 1602;
        }

        return 1603;
    }
}
