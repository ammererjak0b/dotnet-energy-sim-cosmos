using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Time;

/// <summary>Accepts device registrations and fans clock changes and heartbeats out to participants.</summary>
public sealed class ClockServiceHost : IDisposable
{
    private readonly TimeAuthority _authority;
    private readonly TimeProvider _timeProvider;
    private readonly TimeServiceOptions _options;
    private readonly ClockModbusServer _server;
    private readonly object _lock = new object();
    private readonly Dictionary<int, ClockParticipant> _participants = new Dictionary<int, ClockParticipant>();
    private readonly Dictionary<int, int> _failedHeartbeatCounts = new Dictionary<int, int>();
    private bool _started;

    public event Action<string>? StatusChanged;

    public List<ClockParticipantView> getParticipantViews()
    {
        List<ClockParticipantView> views = new List<ClockParticipantView>();
        lock (_lock)
        {
            foreach (ClockParticipant participant in _participants.Values)
            {
                ClockParticipantView view = new ClockParticipantView();
                view.DeviceId = participant.DeviceId;
                view.Port = participant.Port;
                views.Add(view);
            }
        }

        return views;
    }

    public ClockServiceHost(TimeAuthority authority, TimeProvider timeProvider, TimeServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);
        options.validate();
        ClockEndpointOptions endpointOptions = new ClockEndpointOptions();
        endpointOptions.Port = options.Port;
        endpointOptions.DeviceId = "time-service";
        endpointOptions.validate();
        _authority = authority;
        _timeProvider = timeProvider;
        _options = options;
        _server = new ClockModbusServer(endpointOptions);
    }

    public async Task runAsync(CancellationToken cancellationToken)
    {
        ClockServiceInfo serviceInfo = new ClockServiceInfo();
        serviceInfo.HeartbeatMilliseconds = _options.HeartbeatMilliseconds;
        serviceInfo.HeartbeatExpirySeconds = _options.HeartbeatExpirySeconds;
        serviceInfo.LeadTimeMilliseconds = _options.LeadTimeMilliseconds;
        int[] serviceRegisters = ClockServiceInfoCodec.encode(serviceInfo);
        for (int index = 0; index < serviceRegisters.Length; index++)
        {
            _server.setInputRegister(ClockServiceInfoCodec.REGISTER_ADDRESS + index, serviceRegisters[index]);
        }

        _server.start();
        _started = true;
        try
        {
            Task registrations = acceptRegistrationsAsync(cancellationToken);
            Task changes = broadcastChangesAsync(cancellationToken);
            Task heartbeats = sendHeartbeatsAsync(cancellationToken);
            await Task.WhenAll(registrations, changes, heartbeats);
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_started)
        {
            _server.Dispose();
            _started = false;
        }

        lock (_lock)
        {
            foreach (ClockParticipant participant in _participants.Values)
            {
                participant.Dispose();
            }

            _participants.Clear();
        }
    }

    private async Task acceptRegistrationsAsync(CancellationToken cancellationToken)
    {
        await foreach (ClockRegisterWrite registration in _server.Writes.ReadAllAsync(cancellationToken))
        {
            if (registration.StartingAddress != 0 || registration.Values.Count != 1)
            {
                continue;
            }

            int port = registration.Values[0];
            if (port < 1024 || port > 65535)
            {
                report("Rejected invalid registration port " + port + ".");
                continue;
            }

            acceptParticipant(port);
        }
    }

    private void acceptParticipant(int port)
    {
        lock (_lock)
        {
            if (_participants.ContainsKey(port))
            {
                return;
            }
        }

        ClockModbusClient identityClient = new ClockModbusClient("127.0.0.1", port, 1);
        string deviceId;
        try
        {
            identityClient.connect();
            deviceId = ClockIdentityCodec.decode(identityClient.readInputRegisters(ClockEndpointServer.IDENTITY_ADDRESS, ClockIdentityCodec.REGISTER_COUNT));
        }
        catch (Exception exception)
        {
            report("Device registration failed on port " + port + ": " + exception.Message);
            identityClient.Dispose();
            return;
        }

        identityClient.Dispose();
        lock (_lock)
        {
            foreach (ClockParticipant existing in _participants.Values)
            {
                if (existing.DeviceId == deviceId)
                {
                    report("Rejected duplicate device ID " + deviceId + ".");
                    return;
                }
            }
        }

        ClockParticipant participant = new ClockParticipant("127.0.0.1", port, 1, deviceId);
        try
        {
            participant.connect();
            ClockDefinition current = _authority.current();
            participant.sendDefinition(current, ClockMessageKind.Initialize);
            ClockDefinition? pending = _authority.pending();
            if (pending != null)
            {
                ClockMessageKind kind = ClockMessageKind.Change;
                if (pending.Generation > current.Generation)
                {
                    kind = ClockMessageKind.Restart;
                }

                participant.sendDefinition(pending, kind);
            }

            lock (_lock)
            {
                _participants[port] = participant;
            }

            report("Registered " + deviceId + " on 127.0.0.1:" + port + ".");
        }
        catch (Exception exception)
        {
            participant.Dispose();
            report("Could not initialize " + deviceId + ": " + exception.Message);
        }

    }

    private async Task broadcastChangesAsync(CancellationToken cancellationToken)
    {
        await foreach (ClockTransition transition in _authority.Changes.ReadAllAsync(cancellationToken))
        {
            List<ClockParticipant> participants = getParticipants();
            foreach (ClockParticipant participant in participants)
            {
                try
                {
                    participant.sendDefinition(transition.Definition, transition.Kind);
                    report("Sent revision " + transition.Definition.Revision + " to " + participant.DeviceId + ".");
                }
                catch (Exception exception)
                {
                    report("Clock delivery to " + participant.DeviceId + " failed: " + exception.Message);
                }
            }
        }
    }

    private async Task sendHeartbeatsAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.HeartbeatMilliseconds), _timeProvider);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            ClockDefinition current = _authority.current();
            ClockDefinition? pending = _authority.pending();
            ClockDefinition heartbeatDefinition = current;
            if (pending != null)
            {
                heartbeatDefinition = pending;
            }

            ClockHeartbeat heartbeat = new ClockHeartbeat();
            heartbeat.SessionId = heartbeatDefinition.SessionId;
            heartbeat.Revision = heartbeatDefinition.Revision;
            heartbeat.IssuedTimestamp = _timeProvider.GetTimestamp();
            foreach (ClockParticipant participant in getParticipants())
            {
                try
                {
                    participant.sendHeartbeat(heartbeat);
                    clearHeartbeatFailures(participant.Port);
                }
                catch (Exception exception)
                {
                    bool removeParticipant = recordHeartbeatFailure(participant.Port);
                    if (removeParticipant)
                    {
                        participant.Dispose();
                        report("Removed unreachable device " + participant.DeviceId + "; it can register again. " + exception.Message);
                        continue;
                    }

                }
            }
        }
    }

    private List<ClockParticipant> getParticipants()
    {
        lock (_lock)
        {
            return new List<ClockParticipant>(_participants.Values);
        }
    }

    private bool recordHeartbeatFailure(int port)
    {
        lock (_lock)
        {
            int failureCount = 0;
            _failedHeartbeatCounts.TryGetValue(port, out failureCount);
            failureCount++;
            if (failureCount >= 3)
            {
                _participants.Remove(port);
                _failedHeartbeatCounts.Remove(port);
                return true;
            }

            _failedHeartbeatCounts[port] = failureCount;
            return false;
        }
    }

    private void clearHeartbeatFailures(int port)
    {
        lock (_lock)
        {
            _failedHeartbeatCounts.Remove(port);
        }
    }

    private void report(string message)
    {
        Action<string>? handler = StatusChanged;
        if (handler != null)
        {
            handler(message);
        }
    }
}
