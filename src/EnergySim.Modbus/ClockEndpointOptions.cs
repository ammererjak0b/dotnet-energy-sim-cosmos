using System.Net;

namespace EnergySim.Modbus;

/// <summary>Local Modbus endpoint identity and bind settings.</summary>
public sealed class ClockEndpointOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Checks unique, unprivileged local clock endpoints.</summary>
    public void validate()
    {
        validateHost(Host);

        if (Port < PortRules.MIN_PORT || Port > PortRules.MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port));
        }

        validateDeviceId(DeviceId);
    }

    /// <summary>Address the endpoint listens on: "localhost" = loopback (default, one machine), an IP like 0.0.0.0 for containers.</summary>
    public IPAddress bindAddress()
    {
        return parseHost(Host);
    }

    /// <summary>Throws ArgumentException unless host is "localhost" or an IP address. One rule for options and command line.</summary>
    public static void validateHost(string host)
    {
        parseHost(host);
    }

    private static IPAddress parseHost(string host)
    {
        if (host == "localhost")
        {
            return IPAddress.Loopback;
        }

        IPAddress? address;
        if (!IPAddress.TryParse(host, out address))
        {
            throw new ArgumentException("Clock endpoint host must be localhost or an IP address, got '" + host + "'.", nameof(host));
        }

        return address;
    }

    /// <summary>Checks the device ID rules alone, used by the identity codec.</summary>
    public static void validateDeviceId(string deviceId)
    {
        if (deviceId.Length == 0 || deviceId.Length > 32)
        {
            throw new ArgumentException("DeviceId must contain 1 to 32 characters.", nameof(deviceId));
        }

        foreach (char character in deviceId)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
            {
                throw new ArgumentException("DeviceId may contain only ASCII letters, digits, hyphen and underscore.", nameof(deviceId));
            }
        }
    }
}
