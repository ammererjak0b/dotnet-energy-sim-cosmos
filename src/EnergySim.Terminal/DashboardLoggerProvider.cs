using Microsoft.Extensions.Logging;

namespace EnergySim.Terminal;

/// <summary>Feeds all logging into a LogBuffer so only the UI task writes to the console.</summary>
public sealed class DashboardLoggerProvider : ILoggerProvider
{
    private readonly LogBuffer _buffer;
    private readonly TimeProvider _timeProvider;

    public DashboardLoggerProvider(LogBuffer buffer, TimeProvider timeProvider)
    {
        _buffer = buffer;
        _timeProvider = timeProvider;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DashboardLogger(_buffer, _timeProvider);
    }

    public void Dispose()
    {
        // nothing to release
    }
}
