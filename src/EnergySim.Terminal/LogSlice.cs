namespace EnergySim.Terminal;

/// <summary>New lines since a sequence number, plus the number to ask with next time.</summary>
public sealed class LogSlice
{
    public LogSlice(IReadOnlyList<string> lines, long nextSequence)
    {
        Lines = lines;
        NextSequence = nextSequence;
    }

    public IReadOnlyList<string> Lines { get; }

    public long NextSequence { get; }
}
