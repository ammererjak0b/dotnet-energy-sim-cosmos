namespace EnergySim.Terminal;

/// <summary>Thread-safe ring buffer of the last N formatted log lines.</summary>
public sealed class LogBuffer
{
    private readonly object _lock = new object();
    private readonly int _capacity;
    private readonly Queue<string> _lines = new Queue<string>();
    private long _totalAdded;

    public LogBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "capacity must be positive");
        }

        _capacity = capacity;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _lines.Count;
            }
        }
    }

    /// <summary>All lines ever added (not only the kept ones).</summary>
    public long TotalAdded
    {
        get
        {
            lock (_lock)
            {
                return _totalAdded;
            }
        }
    }

    public void add(string line)
    {
        lock (_lock)
        {
            _lines.Enqueue(line);
            _totalAdded++;
            while (_lines.Count > _capacity)
            {
                _lines.Dequeue(); // drop oldest
            }
        }
    }

    /// <summary>Newest lines, oldest first.</summary>
    public IReadOnlyList<string> getLast(int count)
    {
        lock (_lock)
        {
            List<string> all = new List<string>(_lines);
            int skip = all.Count - count;
            if (skip <= 0)
            {
                return all;
            }

            return all.GetRange(skip, count);
        }
    }

    /// <summary>Lines added after lastSequence (0 = from the start). Lines already dropped are lost.</summary>
    public LogSlice readSince(long lastSequence)
    {
        lock (_lock)
        {
            long oldestSequence = _totalAdded - _lines.Count; // sequence of first kept line
            long start = Math.Max(lastSequence, oldestSequence);
            List<string> all = new List<string>(_lines);
            int skip = (int)(start - oldestSequence);
            List<string> result = all.GetRange(skip, all.Count - skip);
            return new LogSlice(result, _totalAdded);
        }
    }
}
