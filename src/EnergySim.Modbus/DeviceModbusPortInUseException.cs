using System.Net.Sockets;

namespace EnergySim.Modbus;

/// <summary>A Modbus port is already taken. Is a SocketException so existing catch blocks keep working; the Cli maps it to exit code 1.</summary>
public sealed class DeviceModbusPortInUseException : SocketException
{
    private readonly string _message;

    public DeviceModbusPortInUseException(string host, int port, Exception inner)
        : base((int)SocketError.AddressAlreadyInUse)
    {
        _message = "Modbus port " + host + ":" + port + " is already in use.";
    }

    public override string Message
    {
        get
        {
            return _message;
        }
    }
}
