namespace EnergySim.Core.Engine;

/// <summary>Thread-safe shared timeline; transitions retain their exact effective instants.</summary>
public sealed class ScheduledClock : IClockSource
{
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new object();
    private readonly List<ClockDefinition> _definitions = new List<ClockDefinition>();
    private string _fault = string.Empty;
    private ClockSnapshot _lastSnapshot = new ClockSnapshot();
    private long _lastHeartbeat;
    private bool _monitorHeartbeat;
    private long? _recoveryTimestamp;
    public TimeSpan HeartbeatExpiry { get; set; } = TimeSpan.FromSeconds(3);

    public ScheduledClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>Installs the authoritative mapping on initial registration or reconnection.</summary>
    public void initialize(ClockDefinition definition, bool monitorHeartbeat = false)
    {
        definition.validate();
        lock (_lock)
        {
            if (definition.CounterFrequency != _timeProvider.TimestampFrequency)
            {
                freeze("Incompatible monotonic counter frequency.");
                return;
            }

            _definitions.Clear();
            _definitions.Add(definition.copy());
            _fault = string.Empty;
            _recoveryTimestamp = null;
            _monitorHeartbeat = monitorHeartbeat;
            _lastHeartbeat = _timeProvider.GetTimestamp();
            _lastSnapshot = snapshotAt(_lastHeartbeat);
        }
    }

    /// <summary>Queues an announced transition.</summary>
    public void receive(ClockDefinition definition, bool restart = false)
    {
        definition.validate();
        lock (_lock)
        {
            ClockDefinition previous = _definitions[_definitions.Count - 1];
            if (definition.CounterFrequency != _timeProvider.TimestampFrequency)
            {
                freeze("Monotonic counter frequency mismatch.");
                return;
            }

            if (restart && definition.AnchorTimestamp >= _timeProvider.GetTimestamp()
                && (definition.Generation > previous.Generation || definition.SessionId != previous.SessionId))
            {
                _definitions.Add(definition.copy());
                _recoveryTimestamp = definition.AnchorTimestamp;
                return;
            }

            if (definition.Revision != previous.Revision + 1 || definition.AnchorTimestamp < _timeProvider.GetTimestamp())
            {
                freeze("Late command or missing clock revision; retrying registration.");
                return;
            }

            _definitions.Add(definition.copy());
        }
    }

    public ClockSnapshot getSnapshot()
    {
        lock (_lock)
        {
            if (_recoveryTimestamp.HasValue && _timeProvider.GetTimestamp() >= _recoveryTimestamp.Value)
            {
                _fault = string.Empty;
                _lastHeartbeat = _recoveryTimestamp.Value;
                _recoveryTimestamp = null;
            }

            if (_fault.Length > 0)
            {
                _lastSnapshot.IsSynced = false;
                _lastSnapshot.IsUsingFallback = false;
                _lastSnapshot.SyncError = _fault;
                return _lastSnapshot;
            }

            _lastSnapshot = snapshotAt(_timeProvider.GetTimestamp());
            if (_monitorHeartbeat && _timeProvider.GetElapsedTime(_lastHeartbeat) > HeartbeatExpiry)
            {
                _lastSnapshot.IsSynced = false;
                _lastSnapshot.IsUsingFallback = true;
                _lastSnapshot.SyncError = "Time service connection lost; using last clock settings and retrying connection.";
            }

            return _lastSnapshot;
        }
    }

    /// <summary>Checks service identity and revision; old heartbeats cannot renew the lease.</summary>
    public void heartbeat(Guid sessionId, int revision, long issuedTimestamp)
    {
        lock (_lock)
        {
            if (_definitions.Count == 0)
            {
                return;
            }

            ClockDefinition latest = _definitions[_definitions.Count - 1];
            if (sessionId != latest.SessionId || revision > latest.Revision)
            {
                return;
            }

            if (revision != latest.Revision)
            {
                return;
            }

            if (issuedTimestamp <= _timeProvider.GetTimestamp() && (!_monitorHeartbeat || issuedTimestamp > _lastHeartbeat))
            {
                _lastHeartbeat = issuedTimestamp;
                _monitorHeartbeat = true;
            }
        }
    }

    /// <summary>Preserves the last observed clock until authoritative initialization or restart.</summary>
    public void freeze(string reason)
    {
        lock (_lock)
        {
            _fault = reason;
        }
    }

    /// <summary>Returns old and new state at every crossed boundary, followed by the current state.</summary>
    public IReadOnlyList<ClockSnapshot> readAdvance(ClockCursor cursor)
    {
        lock (_lock)
        {
            long now = _timeProvider.GetTimestamp();
            List<ClockSnapshot> snapshots = new List<ClockSnapshot>();
            ClockSnapshot final = getSnapshot();
            if (!final.IsSynced && !final.IsUsingFallback)
            {
                cursor.Timestamp = now;
                return new List<ClockSnapshot> { final };
            }

            if (cursor.HasValue)
            {
                for (int index = 1; index < _definitions.Count; index++)
                {
                    ClockDefinition definition = _definitions[index];
                    if (definition.AnchorTimestamp > cursor.Timestamp && definition.AnchorTimestamp <= now)
                    {
                        snapshots.Add(_definitions[index - 1].getSnapshot(definition.AnchorTimestamp));
                        snapshots.Add(definition.getSnapshot(definition.AnchorTimestamp));
                    }
                }
            }

            snapshots.Add(final);
            cursor.HasValue = true;
            cursor.Timestamp = now;
            return snapshots;
        }
    }

    private ClockSnapshot snapshotAt(long timestamp)
    {
        if (_definitions.Count == 0)
        {
            ClockSnapshot waiting = new ClockSnapshot();
            waiting.SyncError = "Waiting for time-service initialization.";
            return waiting;
        }

        ClockDefinition current = _definitions[0];
        foreach (ClockDefinition definition in _definitions)
        {
            if (definition.AnchorTimestamp <= timestamp)
            {
                current = definition;
            }
        }

        return current.getSnapshot(timestamp);
    }
}
