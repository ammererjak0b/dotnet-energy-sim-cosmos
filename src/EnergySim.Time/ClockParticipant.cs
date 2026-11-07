using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Time;

/// <summary>One registered simulator and its persistent Modbus TCP connection.</summary>
public sealed class ClockParticipant : IDisposable
{
    private readonly object _lock = new object(); // fields only, never held during I/O
    private readonly object _ioLock = new object(); // one in-flight Modbus call
    private readonly string _host;
    private readonly int _port;
    private readonly byte _unitIdentifier;
    private ClockModbusClient _client;
    private bool _connected;
    private bool _disposed;
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
        _client = new ClockModbusClient(_host, _port, _unitIdentifier);
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
        int[] registers = new int[0];
        callClient(delegate (ClockModbusClient client)
        {
            registers = client.readInputRegisters(ClockVirtualTimeCodec.REGISTER_ADDRESS, ClockVirtualTimeCodec.REGISTER_COUNT);
        });
        ClockVirtualTimeSample sample;
        if (ClockVirtualTimeCodec.tryDecode(registers, out sample) && sample.VirtualUnixMilliseconds != 0) // torn read: try again next heartbeat
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

    public void sendDefinition(ClockDefinition definition, ClockMessageKind kind)
    {
        callClient(delegate (ClockModbusClient client)
        {
            client.writeMultipleRegisters(ClockEndpointServer.DEFINITION_ADDRESS, ClockCodec.encode(definition, kind));
        });
    }

    public void sendHeartbeat(ClockHeartbeat heartbeat)
    {
        callClient(delegate (ClockModbusClient client)
        {
            client.writeMultipleRegisters(ClockEndpointServer.HEARTBEAT_ADDRESS, HeartbeatCodec.encode(heartbeat));
        });
    }

    /// <summary>Closes the connection for good; later calls throw ObjectDisposedException.</summary>
    public void Dispose()
    {
        lock (_ioLock)
        {
            lock (_lock)
            {
                _disposed = true;
            }

            _client.Dispose();
            _connected = false;
        }
    }

    /// <summary>Connects on demand, runs one call, drops the client on failure. Blocking I/O, so only _ioLock is held.</summary>
    private void callClient(Action<ClockModbusClient> call)
    {
        lock (_ioLock)
        {
            lock (_lock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            try
            {
                if (!_connected)
                {
                    _client.connect();
                    _connected = true;
                }

                call(_client);
            }
            catch (Exception)
            {
                _client.Dispose();
                _client = new ClockModbusClient(_host, _port, _unitIdentifier);
                _connected = false;
                throw;
            }
        }
    }
}
