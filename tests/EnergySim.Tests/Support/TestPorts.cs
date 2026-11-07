using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace EnergySim.Tests.Support;

/// <summary>Hands out free loopback ports from a fixed range. Each call moves on, so parallel tests never share a port.</summary>
public static class TestPorts
{
    public const int MODBUS_FIRST = 17000;
    public const int MODBUS_LAST = 17999;
    public const int CLOCK_FIRST = 18000;
    public const int CLOCK_LAST = 18999;
    public const int WEATHER_FIRST = 19000;
    public const int WEATHER_LAST = 19999;

    private static readonly ConcurrentDictionary<int, int> _lastHandedOut = new ConcurrentDictionary<int, int>();

    public static int next(int firstPort, int lastPort)
    {
        int rangeSize = lastPort - firstPort + 1;
        for (int attempt = 0; attempt < rangeSize; attempt++)
        {
            int offset = _lastHandedOut.AddOrUpdate(firstPort, 0, delegate (int key, int last) { return (last + 1) % rangeSize; });
            int port = firstPort + offset;
            if (isFree(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException("No free port in " + firstPort + ".." + lastPort);
    }

    private static bool isFree(int port)
    {
        TcpListener probe = new TcpListener(IPAddress.Loopback, port);
        try
        {
            probe.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            probe.Stop();
        }
    }
}
