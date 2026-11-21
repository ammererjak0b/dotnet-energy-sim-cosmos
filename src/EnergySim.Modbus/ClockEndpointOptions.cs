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
        bindAddress(); // throws for anything but "localhost" or an IP address

        if (Port < PortRules.MIN_PORT || Port > PortRules.MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port));
        }

        validateDeviceId(DeviceId);
    }

    /// <summary>Address the endpoint listens on: "localhost" = loopback (default, one machine), an IP like 0.0.0.0 for containers.</summary>
    public System.Net.IPAddress bindAddress()
    {
        if (Host == "localhost")
        {
            return System.Net.IPAddress.Loopback;
        }

        System.Net.IPAddress? address;
        if (!System.Net.IPAddress.TryParse(Host, out address))
        {
            throw new ArgumentException("Clock endpoint host must be localhost or an IP address, got '" + Host + "'.", nameof(Host));
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
