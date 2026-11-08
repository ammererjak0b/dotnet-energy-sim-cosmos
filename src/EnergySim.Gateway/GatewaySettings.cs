using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Messaging;
using EnergySim.Modbus;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Gateway;

/// <summary>One polled device from appsettings.json.</summary>
public sealed class GatewayDeviceConfig
{
    public GatewayDeviceConfig(string id, DeviceType type, string host, int port)
    {
        Id = id;
        Type = type;
        Host = host;
        Port = port;
    }

    public string Id { get; }

    public DeviceType Type { get; }

    public string Host { get; }

    public int Port { get; }
}

/// <summary>Settings from appsettings.json and the command line (--PollIntervalMs, --UnitId, --Rabbit:Host ...). No secrets, credentials come from the environment.</summary>
public sealed class GatewaySettings
{
    private const int DEFAULT_POLL_INTERVAL_MS = 1000;
    private const int MIN_POLL_INTERVAL_MS = 100;
    private const int MAX_POLL_INTERVAL_MS = 60000;

    private GatewaySettings(int pollIntervalMs, byte unitId, List<GatewayDeviceConfig> devices, RabbitOptions rabbit)
    {
        PollIntervalMs = pollIntervalMs;
        UnitId = unitId;
        Devices = devices;
        Rabbit = rabbit;
    }

    public int PollIntervalMs { get; }

    public byte UnitId { get; }

    public List<GatewayDeviceConfig> Devices { get; }

    public RabbitOptions Rabbit { get; }

    /// <summary>Throws InvalidOperationException for invalid values.</summary>
    public static GatewaySettings load(IConfiguration configuration)
    {
        int pollIntervalMs = readInt(configuration, "PollIntervalMs", DEFAULT_POLL_INTERVAL_MS, MIN_POLL_INTERVAL_MS, MAX_POLL_INTERVAL_MS);
        int unitId = readInt(configuration, "UnitId", PortRules.MIN_UNIT_ID, PortRules.MIN_UNIT_ID, PortRules.MAX_UNIT_ID);
        List<GatewayDeviceConfig> devices = new List<GatewayDeviceConfig>();
        foreach (IConfigurationSection section in configuration.GetSection("Devices").GetChildren())
        {
            devices.Add(readDevice(section));
        }

        if (devices.Count == 0)
        {
            throw new InvalidOperationException("no devices configured (Devices in appsettings.json)");
        }

        foreach (GatewayDeviceConfig device in devices)
        {
            if (devices.FindAll(other => other.Id == device.Id).Count > 1)
            {
                throw new InvalidOperationException("device id '" + device.Id + "' is used twice");
            }
        }

        RabbitOptions rabbit = RabbitOptions.load(configuration);
        return new GatewaySettings(pollIntervalMs, (byte)unitId, devices, rabbit);
    }

    private static GatewayDeviceConfig readDevice(IConfigurationSection section)
    {
        string? id = section["Id"];
        string? host = section["Host"];
        string? typeText = section["Type"];
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(host) || string.IsNullOrEmpty(typeText))
        {
            throw new InvalidOperationException("device " + section.Key + " needs Id, Type and Host");
        }

        DeviceType type;
        if (!Enum.TryParse(typeText, true, out type) || !Enum.IsDefined(typeof(DeviceType), type))
        {
            throw new InvalidOperationException("device '" + id + "': unknown Type '" + typeText + "' (Pv, Battery, Charger, Wind)");
        }

        int port = readInt(section, "Port", 0, PortRules.MIN_PORT, PortRules.MAX_PORT);
        if (port == 0)
        {
            throw new InvalidOperationException("device '" + id + "' needs a Port");
        }

        return new GatewayDeviceConfig(id, type, host, port);
    }

    private static int readInt(IConfiguration configuration, string key, int defaultValue, int min, int max)
    {
        string? text = configuration[key];
        if (string.IsNullOrEmpty(text))
        {
            return defaultValue;
        }

        int value;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) || value < min || value > max)
        {
            throw new InvalidOperationException(key + " '" + text + "' must be a number from " + min + " to " + max);
        }

        return value;
    }
}
