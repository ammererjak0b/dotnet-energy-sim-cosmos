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
        if (Host != "127.0.0.1" && Host != "localhost")
        {
            throw new ArgumentException("Clock endpoints must bind to localhost.", nameof(Host));
        }

        if (Port < 1024 || Port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port));
        }

        if (DeviceId.Length == 0 || DeviceId.Length > 32)
        {
            throw new ArgumentException("DeviceId must contain 1 to 32 characters.", nameof(DeviceId));
        }

        foreach (char character in DeviceId)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-' && character != '_')
            {
                throw new ArgumentException("DeviceId may contain only ASCII letters, digits, hyphen and underscore.", nameof(DeviceId));
            }
        }
    }
}
