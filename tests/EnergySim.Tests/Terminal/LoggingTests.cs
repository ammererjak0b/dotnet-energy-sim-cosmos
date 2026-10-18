using EnergySim.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Terminal;

public class LoggingTests
{
    private static FakeTimeProvider createTime()
    {
        return new FakeTimeProvider(new DateTimeOffset(2026, 6, 21, 4, 0, 5, TimeSpan.Zero));
    }

    [Fact]
    public void Log_ThroughProvider_FeedsBufferWithTimestampLevelMessage()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(10);
        DashboardLoggerProvider provider = new DashboardLoggerProvider(buffer, createTime());
        ILogger logger = provider.CreateLogger("test");

        // Act
        logger.LogWarning("weather {Id}", 7);

        // Assert
        Assert.Equal(new List<string> { "04:00:05 WRN weather 7" }, buffer.getLast(5));
    }

    [Fact]
    public void Log_BelowInformation_IsIgnored()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(10);
        DashboardLoggerProvider provider = new DashboardLoggerProvider(buffer, createTime());
        ILogger logger = provider.CreateLogger("test");

        // Act
        logger.LogDebug("noise");

        // Assert
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Log_WithException_AppendsExceptionMessage()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(10);
        DashboardLoggerProvider provider = new DashboardLoggerProvider(buffer, createTime());
        ILogger logger = provider.CreateLogger("test");

        // Act
        logger.LogError(new InvalidOperationException("boom"), "failed");

        // Assert
        Assert.Equal("04:00:05 ERR failed: boom", buffer.getLast(1)[0]);
    }

    [Fact]
    public void WriteLine_WritesPlainLineWithoutMarkup()
    {
        // Arrange
        StringWriter output = new StringWriter();
        PlainConsoleWriter writer = new PlainConsoleWriter(output, createTime());

        // Act
        writer.writeLine(LogLevel.Information, "[red]hello[/]");

        // Assert
        Assert.Equal("04:00:05 INF [red]hello[/]" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public async Task RunAsync_DrainsBufferToWriterAndStopsOnCancel()
    {
        // Arrange
        FakeTimeProvider time = createTime();
        LogBuffer buffer = new LogBuffer(10);
        StringWriter output = new StringWriter();
        PlainConsoleWriter writer = new PlainConsoleWriter(output, time);
        using CancellationTokenSource cts = new CancellationTokenSource();
        buffer.add("first");
        Task task = writer.runAsync(buffer, TimeSpan.FromMilliseconds(100), cts.Token);

        // Act
        while (!output.ToString().Contains("first"))
        {
            time.Advance(TimeSpan.FromMilliseconds(100));
            await Task.Yield();
        }
        buffer.add("second");
        while (!output.ToString().Contains("second"))
        {
            time.Advance(TimeSpan.FromMilliseconds(100));
            await Task.Yield();
        }
        cts.Cancel();
        await task;

        // Assert
        string text = output.ToString();
        Assert.Equal(1, text.Split("first").Length - 1);
        Assert.Equal(1, text.Split("second").Length - 1);
    }
}
