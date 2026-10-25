using EnergySim.Weather;
using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.Weather;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Cli;

/// <summary>Command line options: --device --id --start --speed --location --csv. Device specific settings are bound in DeviceFactory.</summary>
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

    public string WeatherUrl { get; private set; } = string.Empty; // empty = weather model in this process

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
        settings.WeatherUrl = readWeatherUrl(configuration);
        settings.applyDeviceChoice(settings.Device);
        return settings;
    }

    /// <summary>Picker result: sets the device and the default id when no --id was given.</summary>
    public CliSettings withDevice(string device)
    {
        CliSettings copy = (CliSettings)MemberwiseClone();
        copy.Device = device;
        copy.Id = string.Empty;
        copy.applyDeviceChoice(device);
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

    private static string readWeatherUrl(IConfiguration configuration)
    {
        string text = readText(configuration, "weather-url", string.Empty);
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
}
