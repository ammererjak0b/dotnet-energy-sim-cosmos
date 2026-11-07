namespace EnergySim.Modbus;

/// <summary>Local Modbus endpoint identity and bind settings.</summary>
public sealed class ClockEndpointOptions
{
    public const int MIN_PORT = 1502; // time service default, devices above
    public const int MAX_PORT = 65535;

    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Checks unique, unprivileged local clock endpoints.</summary>
    public void validate()
    {
        if (Host != "127.0.0.1" && Host != "localhost")
        {
            throw new ArgumentException("Clock endpoints must bind to localhost.", nameof(Host));
        }

        if (Port < MIN_PORT || Port > MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port));
        }

        validateDeviceId(DeviceId);
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
