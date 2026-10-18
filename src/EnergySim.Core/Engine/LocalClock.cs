namespace EnergySim.Core.Engine;

/// <summary>
/// Clock of a single process, driven by a TimeProvider. Virtual time is always computed from the anchor
/// (virtual time at the last change + real elapsed * speed), never summed up tick by tick.
/// Pause, resume and speed change re-anchor, so virtual time never jumps. Can later be replaced by a synced clock.
/// </summary>
public sealed class LocalClock : IClockSource
{
    public const double MIN_SPEED = 0.1;

    public const double MAX_SPEED = 3600.0;

    private readonly object _lock = new object();
    private readonly TimeProvider _timeProvider;
    private long _anchorTimestamp; // real time of the last change
    private DateTimeOffset _anchorVirtualTime; // virtual time at the last change
    private double _speed;
    private ClockStatus _status = ClockStatus.Running;
    private int _generation = 1;

    public LocalClock(TimeProvider timeProvider, DateTimeOffset startTime, double speed)
    {
        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        requireValidSpeed(speed);
        _timeProvider = timeProvider;
        _anchorTimestamp = timeProvider.GetTimestamp();
        _anchorVirtualTime = startTime;
        _speed = speed;
    }

    public ClockSnapshot getSnapshot()
    {
        lock (_lock)
        {
            ClockSnapshot snapshot = new ClockSnapshot();
            snapshot.VirtualNow = calculateVirtualNow();
            snapshot.Status = _status;
            snapshot.Speed = _speed;
            snapshot.Generation = _generation;
            snapshot.IsSynced = true;
            return snapshot;
        }
    }

    public void pause()
    {
        lock (_lock)
        {
            if (_status != ClockStatus.Running)
            {
                return;
            }

            reanchor();
            _status = ClockStatus.Paused;
        }
    }

    public void resume()
    {
        lock (_lock)
        {
            if (_status != ClockStatus.Paused)
            {
                return;
            }

            reanchor(); // real time of the pause is dropped
            _status = ClockStatus.Running;
        }
    }

    public void setSpeed(double speed)
    {
        requireValidSpeed(speed);
        lock (_lock)
        {
            reanchor();
            _speed = speed;
        }
    }

    /// <summary>Jump to a new virtual time. New generation: devices restart there.</summary>
    public void setTime(DateTimeOffset virtualTime)
    {
        lock (_lock)
        {
            _anchorTimestamp = _timeProvider.GetTimestamp();
            _anchorVirtualTime = virtualTime;
            _generation++;
        }
    }

    private DateTimeOffset calculateVirtualNow()
    {
        if (_status != ClockStatus.Running)
        {
            return _anchorVirtualTime;
        }

        TimeSpan realElapsed = _timeProvider.GetElapsedTime(_anchorTimestamp);
        return _anchorVirtualTime + realElapsed * _speed;
    }

    private void reanchor()
    {
        _anchorVirtualTime = calculateVirtualNow(); // freeze current virtual time
        _anchorTimestamp = _timeProvider.GetTimestamp();
    }

    private static void requireValidSpeed(double speed)
    {
        if (double.IsNaN(speed) || speed < MIN_SPEED || speed > MAX_SPEED)
        {
            throw new ArgumentOutOfRangeException(nameof(speed), speed, "speed must be between " + MIN_SPEED + " and " + MAX_SPEED + ".");
        }
    }
}
