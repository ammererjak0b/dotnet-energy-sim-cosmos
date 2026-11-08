using System.Net;
using System.Net.Sockets;

namespace EnergySim.Modbus;

/// <summary>EasyModbus binds on its own thread and kills the process on a busy port: probe first.</summary>
public static class PortProbe
{
    /// <summary>Throws DeviceModbusPortInUseException if taken, other SocketErrors pass through. Small race until the real bind, accepted.</summary>
    public static void ensureFree(IPAddress address, int port)
    {
        TcpListener probe = new TcpListener(address, port);
        try
        {
            probe.Start();
        }
        catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new DeviceModbusPortInUseException(address.ToString(), port, exception);
        }
        finally
        {
            probe.Stop();
        }
    }
}
