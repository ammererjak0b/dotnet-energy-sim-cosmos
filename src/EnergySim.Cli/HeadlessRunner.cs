using EnergySim.Terminal;
using Microsoft.Extensions.Logging;

namespace EnergySim.Cli;

/// <summary>
/// Output or input redirected: no dashboard. One status line per second, log lines as plain text.
/// The PlainConsoleWriter is the only writer (status lines go through the LogBuffer). "quit" or end of stdin ends it.
/// </summary>
public static class HeadlessRunner
{
    private static readonly TimeSpan STATUS_INTERVAL = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan FLUSH_INTERVAL = TimeSpan.FromMilliseconds(200);

    /// <summary>Returns after shutdown.Cancel() (quit command, end of input or Ctrl+C).</summary>
    public static async Task runAsync(IDashboardSource source, LogBuffer logBuffer, TextWriter output, TextReader input, TimeProvider timeProvider, CancellationTokenSource shutdown)
    {
        PlainConsoleWriter writer = new PlainConsoleWriter(output, timeProvider);
        Task printTask = writer.runAsync(logBuffer, FLUSH_INTERVAL, shutdown.Token);
        Task statusTask = writeStatusLinesAsync(source, logBuffer, timeProvider, shutdown.Token);
        Task quitTask = waitForQuitAsync(input, shutdown); // not awaited: a blocked console read cannot be cancelled

        await Task.WhenAll(printTask, statusTask);
        GC.KeepAlive(quitTask);
    }

    private static async Task waitForQuitAsync(TextReader input, CancellationTokenSource shutdown)
    {
        StdinQuitWatcher watcher = new StdinQuitWatcher(input);
        bool quitSeen = await watcher.waitForQuitAsync(shutdown.Token);
        if (quitSeen)
        {
            shutdown.Cancel();
        }
    }

    private static async Task writeStatusLinesAsync(IDashboardSource source, LogBuffer logBuffer, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string line = StatusLine.format(source.getState());
                logBuffer.add(LogLineFormat.format(timeProvider.GetUtcNow(), LogLevel.Information, line));
                await Task.Delay(STATUS_INTERVAL, timeProvider, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal end
        }
    }
}
