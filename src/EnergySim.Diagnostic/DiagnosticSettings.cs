using System.Globalization;
using EnergySim.Modbus;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Diagnostic;

/// <summary>Settings from appsettings.json and the command line (--Host, --Port, --UnitId). No secrets.</summary>
public sealed class DiagnosticSettings
{
    public const int DEFAULT_UNIT_ID = 1;
    public const int MIN_CLIENT_PORT = 1024; // unprivileged ports, client may target any device
    public const int CLIENT_MIN_UNIT_ID = 0; // 0 is answered by the server
    public const int CLIENT_MAX_UNIT_ID = 255; // one byte

    public static readonly IReadOnlyList<string> KNOWN_KEYS = new List<string> { "Host", "Port", "UnitId" };

    private DiagnosticSettings(byte unitId, List<DiagnosticTarget> targets)
    {
        UnitId = unitId;
        Targets = targets;
        StartTarget = targets[0];
    }

    public byte UnitId { get; }

    public List<DiagnosticTarget> Targets { get; }

    public DiagnosticTarget StartTarget { get; private set; }

    /// <summary>Throws DiagnosticUsageException for invalid values.</summary>
    public static DiagnosticSettings load(IConfiguration configuration)
    {
        byte unitId = (byte)readInt(configuration, "UnitId", DEFAULT_UNIT_ID, CLIENT_MIN_UNIT_ID, CLIENT_MAX_UNIT_ID);
        List<DiagnosticTarget> targets = new List<DiagnosticTarget>();
        foreach (IConfigurationSection section in configuration.GetSection("Targets").GetChildren())
        {
            string name = readText(section, "Name");
            string host = readText(section, "Host");
            int port = readInt(section, "Port", 0, MIN_CLIENT_PORT, PortRules.MAX_PORT);
            if (name.Length == 0 || host.Length == 0 || port == 0)
            {
                throw new DiagnosticUsageException("Targets: every entry needs Name, Host and Port");
            }

            targets.Add(new DiagnosticTarget(name, host, port));
        }

        if (targets.Count == 0)
        {
            throw new DiagnosticUsageException("Targets: appsettings.json has no targets");
        }

        DiagnosticSettings settings = new DiagnosticSettings(unitId, targets);
        string hostOverride = readText(configuration, "Host");
        string portOverride = readText(configuration, "Port");
        if (hostOverride.Length > 0 || portOverride.Length > 0)
        {
            string host = settings.StartTarget.Host;
            if (hostOverride.Length > 0)
            {
                host = hostOverride;
            }

            int port = settings.StartTarget.Port;
            if (portOverride.Length > 0)
            {
                port = readInt(configuration, "Port", port, MIN_CLIENT_PORT, PortRules.MAX_PORT);
            }

            settings.StartTarget = new DiagnosticTarget("custom", host, port);
            settings.Targets.Insert(0, settings.StartTarget);
        }

        return settings;
    }

    /// <summary>Only --Key value pairs with a known key are allowed.</summary>
    public static void checkArguments(string[] args)
    {
        int index = 0;
        while (index < args.Length)
        {
            string key = args[index].TrimStart('-');
            if (!args[index].StartsWith("--") || !KNOWN_KEYS.Contains(key))
            {
                throw new DiagnosticUsageException("unknown argument '" + args[index] + "'");
            }

            if (index + 1 >= args.Length)
            {
                throw new DiagnosticUsageException("--" + key + " needs a value");
            }

            index += 2;
        }
    }

    private static string readText(IConfiguration configuration, string key)
    {
        string? value = configuration[key];
        if (value == null)
        {
            return string.Empty;
        }

        return value.Trim();
    }

    private static int readInt(IConfiguration configuration, string key, int fallback, int min, int max)
    {
        string text = readText(configuration, key);
        if (text.Length == 0)
        {
            return fallback;
        }

        int value;
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < min || value > max)
        {
            throw new DiagnosticUsageException(key + ": '" + text + "' must be a number between " + min + " and " + max);
        }

        return value;
    }
}
