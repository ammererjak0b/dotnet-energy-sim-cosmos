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
    private ClockParticipantStatus _status = ClockParticipantStatus.New;
    private DateTimeOffset _lastSeenUtc;
    private int _missedHeartbeats;
    private bool _hasSkew;
    private double _skewMilliseconds;

    public string DeviceId { get; }
    public int Port
    {
        get
        {
            return _port;
        }
    }

    public ClockParticipant(string host, int port, byte unitIdentifier, string deviceId, DateTimeOffset registeredUtc)
    {
        _lastSeenUtc = registeredUtc;
        _host = host;
        _port = port;
        _unitIdentifier = unitIdentifier;
        DeviceId = deviceId;
        _client = createClient();
    }

    /// <summary>Snapshot of status, last seen and skew for the dashboard.</summary>
    public ClockParticipantView createView()
    {
        lock (_lock)
        {
            ClockParticipantView view = new ClockParticipantView();
            view.DeviceId = DeviceId;
            view.Port = _port;
            view.Status = _status;
            view.LastSeenUtc = _lastSeenUtc;
            view.MissedHeartbeats = _missedHeartbeats;
            view.HasSkew = _hasSkew;
            view.SkewMilliseconds = _skewMilliseconds;
            return view;
        }
    }

    /// <summary>Device registered again (restart or retry): back to NEW until a heartbeat arrives.</summary>
    public void markRegistered(DateTimeOffset nowUtc)
    {
        lock (_lock)
        {
            _status = ClockParticipantStatus.New;
            _lastSeenUtc = nowUtc;
            _missedHeartbeats = 0;
            _hasSkew = false;
        }
    }

    /// <summary>Returns true when the status changed (NEW or UNREACHABLE to OK).</summary>
    public bool markHeartbeatDelivered(DateTimeOffset nowUtc)
    {
        lock (_lock)
        {
            bool changed = _status != ClockParticipantStatus.Ok;
            _status = ClockParticipantStatus.Ok;
            _lastSeenUtc = nowUtc;
            _missedHeartbeats = 0;
            return changed;
        }
    }

    /// <summary>Returns true when this miss turned the participant UNREACHABLE.</summary>
    public bool markHeartbeatMissed(int maxMissedHeartbeats)
    {
        lock (_lock)
        {
            _missedHeartbeats++;
            if (_missedHeartbeats >= maxMissedHeartbeats && _status != ClockParticipantStatus.Unreachable)
            {
                _status = ClockParticipantStatus.Unreachable;
                _hasSkew = false; // stale value would mislead
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Reads the device's own virtual time and stores skew against the master timeline.
    /// The read is also the liveness proof: EasyModbus writes to a dead peer do not fail, reads do. Throws when unreachable.
    /// </summary>
    public void updateSkew(TimeAuthority authority)
    {
        bool hasSkew = false;
        double skewMilliseconds = 0;
        ClockVirtualTimeSample? sample = readVirtualTime();
        if (sample != null && sample.VirtualUnixMilliseconds != 0)
        {
            long masterUnixMilliseconds = authority.virtualNowAt(sample.SampledTimestamp).ToUnixTimeMilliseconds();
            skewMilliseconds = sample.VirtualUnixMilliseconds - masterUnixMilliseconds;
            hasSkew = true;
        }

        lock (_lock)
        {
            _hasSkew = hasSkew;
            _skewMilliseconds = skewMilliseconds;
        }
    }

    private ClockVirtualTimeSample? readVirtualTime()
    {
        lock (_lock)
        {
            ensureConnected();
            int[] registers;
            try
            {
                registers = _client.readInputRegisters(ClockVirtualTimeCodec.REGISTER_ADDRESS, ClockVirtualTimeCodec.REGISTER_COUNT);
            }
            catch (Exception)
            {
                resetClient();
                throw;
            }

            ClockVirtualTimeSample sample;
            if (!ClockVirtualTimeCodec.tryDecode(registers, out sample))
            {
                return null; // torn read, try again next heartbeat
            }

            return sample;
        }
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
