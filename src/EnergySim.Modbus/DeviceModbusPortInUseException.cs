namespace EnergySim.Modbus;

/// <summary>The device Modbus port is already taken. The Cli maps this to exit code 1.</summary>
public sealed class DeviceModbusPortInUseException : Exception
{
    public DeviceModbusPortInUseException(string host, int port, Exception inner)
        : base("Modbus port " + host + ":" + port + " is already in use.", inner)
    {
    }
}
