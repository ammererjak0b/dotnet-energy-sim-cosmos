using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

/// <summary>Scripted keys, no real keyboard.</summary>
public class FakeKeySource : IKeySource
{
    private readonly Queue<char> _keys = new Queue<char>();

    public void enqueue(string keys)
    {
        foreach (char key in keys)
        {
            _keys.Enqueue(key);
        }
    }

    public bool tryReadKey(out char key)
    {
        key = ' ';
        if (_keys.Count == 0)
        {
            return false;
        }

        key = _keys.Dequeue();
        return true;
    }
}
