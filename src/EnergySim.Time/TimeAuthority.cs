using EnergySim.Core.Engine;
using EnergySim.Modbus;
using System.Threading.Channels;

namespace EnergySim.Time;

/// <summary>Owns one clock and prepares future changes without any network or console access.</summary>
public sealed class TimeAuthority
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _leadTime;
    private readonly object _lock = new object();
    private ClockDefinition _current;
    private ClockDefinition? _pending;
    private ClockDefinition? _previous; // kept so a sample taken before the latest anchor maps correctly
    private readonly Channel<ClockTransition> _changes = Channel.CreateUnbounded<ClockTransition>();

    public ChannelReader<ClockTransition> Changes
    {
        get
        {
            return _changes.Reader;
        }
    }

    public ScheduledClock Clock { get; }

    public TimeAuthority(TimeProvider timeProvider, DateTimeOffset start, double speed, TimeSpan leadTime)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (leadTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leadTime));
        }

        _timeProvider = timeProvider;
        _leadTime = leadTime;
        _current = new ClockDefinition();
        _current.SessionId = Guid.NewGuid();
        _current.CounterFrequency = timeProvider.TimestampFrequency;
        _current.AnchorTimestamp = timeProvider.GetTimestamp();
        _current.VirtualAnchor = start.ToUniversalTime();
        _current.Speed = speed;
        _current.validate();
        Clock = new ScheduledClock(timeProvider);
        Clock.initialize(_current);
    }

    /// <summary>Prepares the next clock transition.</summary>
    public ClockDefinition execute(ClockAction action, double speed, DateTimeOffset restartTime)
    {
        lock (_lock)
        {
            promotePending();
            if (_pending != null)
            {
                throw new InvalidOperationException("A clock change is already pending.");
            }

            ClockDefinition change = _current.copy();
            change.Revision++;
            change.AnchorTimestamp = _timeProvider.GetTimestamp() + (long)(_leadTime.TotalSeconds * _timeProvider.TimestampFrequency);
            change.VirtualAnchor = _current.getSnapshot(change.AnchorTimestamp).VirtualNow;
            validateAction(action, speed);
            switch (action)
            {
                case ClockAction.Start:
                case ClockAction.Resume:
                    change.Status = ClockStatus.Running;
                    break;
                case ClockAction.Pause:
                    change.Status = ClockStatus.Paused;
                    break;
                case ClockAction.SetSpeed:
                    change.Speed = speed;
                    break;
                case ClockAction.Restart:
                    change.Generation++;
                    change.VirtualAnchor = restartTime.ToUniversalTime();
                    change.Status = ClockStatus.Running;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }

            change.validate();
            _pending = change;
            Clock.receive(change, action == ClockAction.Restart);
            ClockMessageKind messageKind = ClockMessageKind.Change;
            if (action == ClockAction.Restart)
            {
                messageKind = ClockMessageKind.Restart;
            }

            _changes.Writer.TryWrite(new ClockTransition(change, messageKind));
            return change.copy();
        }
    }

    /// <summary>Returns the active definition for a newly connected participant.</summary>
    public ClockDefinition current()
    {
        lock (_lock)
        {
            promotePending();
            return _current.copy();
        }
    }

    /// <summary>Returns a copy of the announced change, if any.</summary>
    public ClockDefinition? pending()
    {
        lock (_lock)
        {
            promotePending();
            if (_pending == null)
            {
                return null;
            }

            return _pending.copy();
        }
    }

    /// <summary>Master virtual time at a past or future monotonic instant (pending change applies once its anchor is reached).</summary>
    public DateTimeOffset virtualNowAt(long timestamp)
    {
        lock (_lock)
        {
            promotePending();
            ClockDefinition definition = _current;
            if (_pending != null && timestamp >= _pending.AnchorTimestamp)
            {
                definition = _pending;
            }
            else if (_previous != null && timestamp < _current.AnchorTimestamp)
            {
                definition = _previous;
            }

            return definition.getSnapshot(timestamp).VirtualNow;
        }
    }

    private void promotePending()
    {
        if (_pending != null && _timeProvider.GetTimestamp() >= _pending.AnchorTimestamp)
        {
            _previous = _current;
            _current = _pending;
            _pending = null;
        }
    }

    private void validateAction(ClockAction action, double speed)
    {
        bool isRunning = _current.Status == ClockStatus.Running;
        bool isPaused = _current.Status == ClockStatus.Paused;
        bool isStopped = _current.Status == ClockStatus.Stopped;

        if (action == ClockAction.Start && !isStopped)
        {
            throw new InvalidOperationException("Clock can only start from stopped state.");
        }

        if (action == ClockAction.Pause && !isRunning)
        {
            throw new InvalidOperationException("Clock can only pause while running.");
        }

        if (action == ClockAction.Resume && !isPaused)
        {
            throw new InvalidOperationException("Clock can only resume while paused.");
        }

        if (action == ClockAction.SetSpeed)
        {
            if (isStopped)
            {
                throw new InvalidOperationException("Clock speed can only change after start.");
            }

            if (!double.IsFinite(speed) || speed < LocalClock.MIN_SPEED || speed > LocalClock.MAX_SPEED)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }
    }
}
