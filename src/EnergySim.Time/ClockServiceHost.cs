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
    private const string LOCAL_HOST = "127.0.0.1";
    private const byte DEVICE_UNIT_ID = 1;

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

        List<ClockParticipant> participants;
        lock (_lock)
        {
            participants = new List<ClockParticipant>(_participants.Values);
            _participants.Clear();
        }

        foreach (ClockParticipant participant in participants)
        {
            participant.Dispose(); // outside the lock, may wait for one Modbus call
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
            if (port < PortRules.MIN_PORT || port > PortRules.MAX_PORT)
            {
                report("Rejected registration port " + port + " (allowed " + PortRules.MIN_PORT + " to " + PortRules.MAX_PORT + ").");
                continue;
            }

            if (port == _options.Port)
            {
                report("Rejected registration port " + port + " (own service port).");
                continue;
            }

            acceptParticipant(port);
        }
    }

    private void acceptParticipant(int port)
    {
        string deviceId;
        try
        {
            deviceId = readDeviceId(port);
        }
        catch (Exception exception)
        {
            report("Device registration failed on port " + port + ": " + exception.Message);
            return;
        }

        ClockParticipant? existingParticipant;
        lock (_lock)
        {
            _participants.TryGetValue(port, out existingParticipant);
        }

        if (existingParticipant != null && existingParticipant.DeviceId == deviceId)
        {
            resynchronize(existingParticipant, port);
            return;
        }

        if (existingParticipant != null)
        {
            removeParticipant(existingParticipant);
            report("Port " + port + " now answers as " + deviceId + ", replaced " + existingParticipant.DeviceId + ".");
        }

        ClockParticipant? holder = findHolder(deviceId);
        if (holder != null && holder.createView().Status != ClockParticipantStatus.Unreachable)
        {
            string duplicateReport = "Rejected duplicate device ID " + deviceId + " on port " + port + ".";
            if (duplicateReport != _lastDuplicateReport)
            {
                report(duplicateReport);
                _lastDuplicateReport = duplicateReport;
            }

            return;
        }

        _lastDuplicateReport = string.Empty; // condition cleared, report again if it returns
        if (holder != null)
        {
            removeParticipant(holder);
            report("Replaced unreachable " + deviceId + " on port " + holder.Port + " by port " + port + ".");
        }

        addParticipant(deviceId, port);
    }

    private void resynchronize(ClockParticipant participant, int port)
    {
        try
        {
            initializeParticipant(participant);
            participant.markRegistered(_timeProvider.GetUtcNow());
            report("Re-registered " + participant.DeviceId + " on port " + port + "; resynchronized.");
        }
        catch (Exception exception)
        {
            report("Could not resynchronize " + participant.DeviceId + ": " + exception.Message);
        }
    }

    private void addParticipant(string deviceId, int port)
    {
        ClockParticipant participant = new ClockParticipant(LOCAL_HOST, port, DEVICE_UNIT_ID, deviceId, _timeProvider.GetUtcNow());
        try
        {
            initializeParticipant(participant); // also opens the connection
        }
        catch (Exception exception)
        {
            participant.Dispose();
            report("Could not initialize " + deviceId + ": " + exception.Message);
            return;
        }

        lock (_lock)
        {
            _participants[port] = participant;
        }

        report("Registered " + deviceId + " on " + LOCAL_HOST + ":" + port + ".");
    }

    private static string readDeviceId(int port)
    {
        using ClockModbusClient identityClient = new ClockModbusClient(LOCAL_HOST, port, DEVICE_UNIT_ID);
        identityClient.connect();
        return ClockIdentityCodec.decode(identityClient.readInputRegisters(ClockEndpointServer.IDENTITY_ADDRESS, ClockIdentityCodec.REGISTER_COUNT));
    }

    /// <summary>The participant using this device ID, or null. Does not change anything.</summary>
    private ClockParticipant? findHolder(string deviceId)
    {
        lock (_lock)
        {
            foreach (ClockParticipant existing in _participants.Values)
            {
                if (existing.DeviceId == deviceId)
                {
                    return existing;
                }
            }
        }

        return null;
    }

    private void removeParticipant(ClockParticipant participant)
    {
        lock (_lock)
        {
            _participants.Remove(participant.Port);
        }

        participant.Dispose(); // outside the lock
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
                catch (ObjectDisposedException)
                {
                    continue; // participant was replaced meanwhile
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
                heartbeatParticipant(participant, heartbeat);
            }
        }
    }

    private void heartbeatParticipant(ClockParticipant participant, ClockHeartbeat heartbeat)
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
        catch (ObjectDisposedException)
        {
            return; // participant was replaced meanwhile
        }
        catch (Exception exception)
        {
            if (participant.markHeartbeatMissed(MAX_MISSED_HEARTBEATS))
            {
                report(participant.DeviceId + " is UNREACHABLE after " + MAX_MISSED_HEARTBEATS + " missed heartbeats; retrying. " + exception.Message);
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
