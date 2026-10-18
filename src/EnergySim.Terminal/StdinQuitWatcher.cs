namespace EnergySim.Terminal;

/// <summary>
/// Graceful shutdown signal for programs started by the master (headless): reading the line "quit" from stdin,
/// or end of input, requests shutdown. Async line reading, no blocking.
/// </summary>
public sealed class StdinQuitWatcher
{
    public const string QUIT_COMMAND = "quit";

    private readonly TextReader _reader;

    public StdinQuitWatcher(TextReader reader)
    {
        _reader = reader;
    }

    /// <summary>Completes (true) when quit or end of input was seen, false when cancelled before.</summary>
    public async Task<bool> waitForQuitAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await _reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    return true; // end of input, parent gone
                }

                if (string.Equals(line.Trim(), QUIT_COMMAND, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return false;
    }
}
