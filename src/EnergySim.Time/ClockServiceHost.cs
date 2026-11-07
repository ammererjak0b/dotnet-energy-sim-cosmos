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
    private bool _started;
    private string _lastDuplicateReport = string.Empty; // devices retry every second, report once

    public const int MAX_MISSED_HEARTBEATS = 3; // then UNREACHABLE

    public event Action<string>? StatusChanged;

    public List<ClockParticipantView> getParticipantViews()
    {
        List<ClockParticipantView> views = new List<ClockParticipantView>();
        lock (_lock)
        {
            foreach (ClockParticipant participant in _participants.Values)
            {
                views.Add(participant.createView());
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
            if (port < ClockEndpointOptions.MIN_PORT || port > ClockEndpointOptions.MAX_PORT)
            {
                report("Rejected registration port " + port + " (allowed " + ClockEndpointOptions.MIN_PORT + " to " + ClockEndpointOptions.MAX_PORT + ").");
                continue;
            }

            acceptParticipant(port);
        }
    }

    private void acceptParticipant(int port)
    {
        ClockParticipant? existingParticipant;
        lock (_lock)
        {
            _participants.TryGetValue(port, out existingParticipant);
        }

        if (existingParticipant != null)
        {
            try
            {
                initializeParticipant(existingParticipant);
                existingParticipant.markRegistered(_timeProvider.GetUtcNow());
                report("Re-registered " + existingParticipant.DeviceId + " on port " + port + "; resynchronized.");
            }
            catch (Exception exception)
            {
                report("Could not resynchronize " + existingParticipant.DeviceId + ": " + exception.Message);
            }

            return;
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
        if (isDuplicateDeviceId(deviceId, port))
        {
            string duplicateReport = "Rejected duplicate device ID " + deviceId + " on port " + port + ".";
            if (duplicateReport != _lastDuplicateReport)
            {
                report(duplicateReport);
                _lastDuplicateReport = duplicateReport;
            }

            return;
        }

        ClockParticipant participant = new ClockParticipant("127.0.0.1", port, 1, deviceId, _timeProvider.GetUtcNow());
        try
        {
            participant.connect();
            initializeParticipant(participant);

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

    /// <summary>True when a live participant already uses the ID; an UNREACHABLE holder is replaced (device moved port).</summary>
    private bool isDuplicateDeviceId(string deviceId, int newPort)
    {
        lock (_lock)
        {
            int stalePort = -1;
            foreach (ClockParticipant existing in _participants.Values)
            {
                if (existing.DeviceId != deviceId)
                {
                    continue;
                }

                if (existing.createView().Status != ClockParticipantStatus.Unreachable)
                {
                    return true;
                }

                stalePort = existing.Port;
            }

            if (stalePort >= 0)
            {
                _participants[stalePort].Dispose();
                _participants.Remove(stalePort);
                report("Replaced unreachable " + deviceId + " on port " + stalePort + " by port " + newPort + ".");
            }

            return false;
        }
    }

    /// <summary>Sends the active mapping and any pending change on registration or retry.</summary>
    private void initializeParticipant(ClockParticipant participant)
    {
        ClockDefinition current = _authority.current();
        participant.sendDefinition(current, ClockMessageKind.Initialize);
        ClockDefinition? pending = _authority.pending();
        if (pending == null)
        {
            return;
        }

        ClockMessageKind kind = ClockMessageKind.Change;
        if (pending.Generation > current.Generation)
        {
            kind = ClockMessageKind.Restart;
        }

        participant.sendDefinition(pending, kind);
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
                    participant.updateSkew(_authority); // read doubles as liveness proof
                    if (participant.markHeartbeatDelivered(_timeProvider.GetUtcNow()))
                    {
                        report(participant.DeviceId + " is OK.");
                    }
                }
                catch (Exception exception)
                {
                    if (participant.markHeartbeatMissed(MAX_MISSED_HEARTBEATS))
                    {
                        report(participant.DeviceId + " is UNREACHABLE after " + MAX_MISSED_HEARTBEATS + " missed heartbeats; retrying. " + exception.Message);
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

    private void report(string message)
    {
        Action<string>? handler = StatusChanged;
        if (handler != null)
        {
            handler(message);
        }
    }
}
