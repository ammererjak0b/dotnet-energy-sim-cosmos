using Microsoft.Extensions.Logging;

namespace EnergySim.Terminal;

/// <summary>Headless output: plain lines "HH:mm:ss LVL message", no Spectre markup. The only writer in headless mode.</summary>
public sealed class PlainConsoleWriter
{
    private readonly TextWriter _writer;
    private readonly TimeProvider _timeProvider;

    public PlainConsoleWriter(TextWriter writer, TimeProvider timeProvider)
    {
        _writer = writer;
        _timeProvider = timeProvider;
    }

    public void writeLine(LogLevel level, string message)
    {
        _writer.WriteLine(LogLineFormat.format(_timeProvider.GetUtcNow(), level, message));
    }

    /// <summary>Prints every new buffer line (already formatted) until cancelled.</summary>
    public async Task runAsync(LogBuffer buffer, TimeSpan pollInterval, CancellationToken cancellationToken)
    {
        long nextSequence = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                nextSequence = await flushAsync(buffer, nextSequence, cancellationToken);
                await Task.Delay(pollInterval, _timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // normal end
        }

        await flushAsync(buffer, nextSequence, CancellationToken.None); // last lines
    }

    private async Task<long> flushAsync(LogBuffer buffer, long fromSequence, CancellationToken cancellationToken)
    {
        LogSlice slice = buffer.readSince(fromSequence);
        foreach (string line in slice.Lines)
        {
            await _writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        return slice.NextSequence;
    }
}
