namespace EnergySim.Modbus;

/// <summary>Decoded registration: callback port and the device id the device announced (empty if not announced).</summary>
public sealed class ClockRegistration
{
    public ClockRegistration(int port, string deviceId)
    {
        Port = port;
        DeviceId = deviceId;
    }

    public int Port { get; }
    public string DeviceId { get; }
}
