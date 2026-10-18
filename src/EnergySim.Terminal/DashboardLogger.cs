using Microsoft.Extensions.Logging;

namespace EnergySim.Terminal;

/// <summary>Logger that only appends to the LogBuffer, never touches the console.</summary>
public sealed class DashboardLogger : ILogger
{
    private readonly LogBuffer _buffer;
    private readonly TimeProvider _timeProvider;

    public DashboardLogger(LogBuffer buffer, TimeProvider timeProvider)
    {
        _buffer = buffer;
        _timeProvider = timeProvider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // scopes not shown
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information && logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);
        if (exception != null)
        {
            message = message + ": " + exception.Message;
        }

        _buffer.add(LogLineFormat.format(_timeProvider.GetUtcNow(), logLevel, message));
    }
}
