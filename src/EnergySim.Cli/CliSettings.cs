using EnergySim.Weather;
using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.Weather;
using System.Net;
using EnergySim.Modbus;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Cli;

/// <summary>Command line options include local/shared clock connection settings.</summary>
public sealed class CliSettings
{
    public const string DEFAULT_WEATHER_URL = "http://localhost:5200"; // WeatherApi.DEFAULT_PORT

    public const string DEFAULT_START = "2026-06-21T04:00:00Z";
    public const double DEFAULT_SPEED = 60.0;
    public const string DEFAULT_LOCATION = "salzburg";
    private const int DEFAULT_TIME_PORT = PortRules.MIN_PORT; // time service
    private const int PV_CLOCK_PORT = 1601;
    private const int BATTERY_CLOCK_PORT = 1602;
    private const int CHARGER_CLOCK_PORT = 1603;
    private const int WIND_CLOCK_PORT = 1604;
    private const int PV_MODBUS_PORT = 1511;
    private const int BATTERY_MODBUS_PORT = 1512;
    private const int CHARGER_MODBUS_PORT = 1513;
    private const int WIND_MODBUS_PORT = 1514;

    public static readonly IReadOnlyList<string> DEVICE_NAMES = new List<string> { "pv", "battery", "charger", "wind" };

    public string Device { get; private set; } = string.Empty; // empty = ask in the picker

    public string Id { get; private set; } = string.Empty;

    public DateTimeOffset Start { get; private set; }

    public double Speed { get; private set; }

    public string Location { get; private set; } = DEFAULT_LOCATION;

    public string CsvPath { get; private set; } = string.Empty; // empty = no CSV

    public string WeatherUrl { get; private set; } = DEFAULT_WEATHER_URL; // weather process, always required for pv and wind
    public string ClockMode { get; private set; } = "local";

    public string TimeHost { get; private set; } = "127.0.0.1";

    public int TimePort { get; private set; } = DEFAULT_TIME_PORT;

    public int ClockPort { get; private set; }

    public string ModbusHost { get; private set; } = DeviceModbusOptions.DEFAULT_HOST; // device Modbus server bind address

    public int ModbusPort { get; private set; } // 0 until the device is known, then default per device

    public byte ModbusUnitId { get; private set; } = 1;

    /// <summary>Throws CliUsageException with the option name for invalid values.</summary>
    public static CliSettings load(IConfiguration configuration)
    {
        CliSettings settings = new CliSettings();
        settings.Device = readText(configuration, "device", string.Empty).ToLowerInvariant();
        if (settings.Device.Length > 0 && !DEVICE_NAMES.Contains(settings.Device))
        {
            throw new CliUsageException("device: unknown '" + settings.Device + "', use " + string.Join("|", DEVICE_NAMES));
        }

        settings.Id = readText(configuration, "id", readText(configuration, "Device:Id", string.Empty));
        settings.Start = readStart(configuration);
        settings.Speed = readSpeed(configuration);
        settings.Location = readText(configuration, "location", DEFAULT_LOCATION).ToLowerInvariant();
        if (!LocationCatalog.ids().Contains(settings.Location))
        {
            throw new CliUsageException("location: unknown '" + settings.Location + "', use " + string.Join("|", LocationCatalog.ids()));
        }

        settings.CsvPath = readText(configuration, "csv", string.Empty);
        settings.WeatherUrl = readWeatherUrl(configuration);
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

        settings.TimePort = readPort(configuration, "time-port", DEFAULT_TIME_PORT);
        settings.ClockPort = readPort(configuration, "clock-port", 0);
        settings.ModbusHost = readModbusHost(configuration);
        settings.ModbusPort = readPort(configuration, "Modbus:Port", 0);
        settings.ModbusUnitId = readUnitId(configuration);
        settings.applyDeviceChoice(settings.Device);
        if (settings.Device.Length > 0)
        {
            settings.applyPortDefaults(settings.Device);
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
        copy.applyPortDefaults(device);
        return copy;
    }

    private void applyPortDefaults(string device)
    {
        if (ClockPort == 0)
        {
            ClockPort = defaultClockPort(device);
        }

        if (ModbusPort == 0)
        {
            ModbusPort = defaultModbusPort(device);
        }
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

    private static string readWeatherUrl(IConfiguration configuration)
    {
        string text = readText(configuration, "weather-url", DEFAULT_WEATHER_URL);
        Uri? uri;
        if (text.Length > 0 && (!Uri.TryCreate(text, UriKind.Absolute, out uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new CliUsageException("weather-url: '" + text + "' is not an http URL, e.g. http://localhost:5200");
        }

        return text;
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
            throw new CliUsageException(key + ": port must be between " + PortRules.MIN_PORT + " and " + PortRules.MAX_PORT);
        }

        if (port == 0 && fallback == 0)
        {
            return 0;
        }

        if (port < PortRules.MIN_PORT || port > PortRules.MAX_PORT)
        {
            throw new CliUsageException(key + ": port must be between " + PortRules.MIN_PORT + " and " + PortRules.MAX_PORT);
        }

        return port;
    }

    private static int defaultClockPort(string device)
    {
        if (device == "pv")
        {
            return PV_CLOCK_PORT;
        }

        if (device == "battery")
        {
            return BATTERY_CLOCK_PORT;
        }

        if (device == "charger")
        {
            return CHARGER_CLOCK_PORT;
        }

        return WIND_CLOCK_PORT;
    }

    private static int defaultModbusPort(string device)
    {
        if (device == "pv")
        {
            return PV_MODBUS_PORT;
        }

        if (device == "battery")
        {
            return BATTERY_MODBUS_PORT;
        }

        if (device == "charger")
        {
            return CHARGER_MODBUS_PORT;
        }

        return WIND_MODBUS_PORT;
    }

    private static string readModbusHost(IConfiguration configuration)
    {
        string text = readText(configuration, "Modbus:Host", DeviceModbusOptions.DEFAULT_HOST);
        IPAddress? address;
        if (!IPAddress.TryParse(text, out address))
        {
            throw new CliUsageException("Modbus:Host: must be an IP address");
        }

        return text;
    }

    private static byte readUnitId(IConfiguration configuration)
    {
        string text = readText(configuration, "Modbus:UnitId", "1");
        byte unitId;
        if (!byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out unitId) || unitId < PortRules.MIN_UNIT_ID || unitId > PortRules.MAX_UNIT_ID)
        {
            throw new CliUsageException("Modbus:UnitId: must be between " + PortRules.MIN_UNIT_ID + " and " + PortRules.MAX_UNIT_ID);
        }

        return unitId;
    }
}
