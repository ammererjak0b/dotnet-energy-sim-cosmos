namespace EnergySim.Terminal;

/// <summary>Non-blocking keyboard.</summary>
public interface IKeySource
{
    /// <summary>False at once when no key is waiting. Never blocks.</summary>
    bool tryReadKey(out char key);
}
