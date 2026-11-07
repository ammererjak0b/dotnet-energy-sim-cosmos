using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Time;

/// <summary>One registered simulator and its persistent Modbus TCP connection.</summary>
public sealed class ClockParticipant : IDisposable
{
    private readonly object _lock = new object();
    private readonly string _host;
    private readonly int _port;
    private readonly byte _unitIdentifier;
    private ClockModbusClient _client;
    private bool _connected;

    public string DeviceId { get; }
    public int Port
    {
        get
        {
            return _port;
        }
    }

    public ClockParticipant(string host, int port, byte unitIdentifier, string deviceId)
    {
        _host = host;
        _port = port;
        _unitIdentifier = unitIdentifier;
        DeviceId = deviceId;
        _client = createClient();
    }

    public void connect()
    {
        lock (_lock)
        {
            _client.connect();
            _connected = true;
        }
    }

    public void sendDefinition(ClockDefinition definition, ClockMessageKind kind)
    {
        lock (_lock)
        {
            ensureConnected();
            try
            {
                _client.writeMultipleRegisters(ClockEndpointServer.DEFINITION_ADDRESS, ClockCodec.encode(definition, kind));
            }
            catch (Exception)
            {
                resetClient();
                throw;
            }
        }
    }

    public void sendHeartbeat(ClockHeartbeat heartbeat)
    {
        lock (_lock)
        {
            ensureConnected();
            try
            {
                _client.writeMultipleRegisters(ClockEndpointServer.HEARTBEAT_ADDRESS, HeartbeatCodec.encode(heartbeat));
            }
            catch (Exception)
            {
                resetClient();
                throw;
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _client.Dispose();
            _connected = false;
        }
    }

    private ClockModbusClient createClient()
    {
        return new ClockModbusClient(_host, _port, _unitIdentifier);
    }

    private void ensureConnected()
    {
        if (!_connected)
        {
            _client.connect();
            _connected = true;
        }
    }

    private void resetClient()
    {
        _client.Dispose();
        _client = createClient();
        _connected = false;
    }
}
