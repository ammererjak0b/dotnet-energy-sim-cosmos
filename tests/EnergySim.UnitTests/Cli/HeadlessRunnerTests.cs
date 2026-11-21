using EnergySim.Cli;
using EnergySim.Core.Devices;
using EnergySim.Terminal;
using EnergySim.UnitTests.Terminal;

namespace EnergySim.UnitTests.Cli;

/// <summary>Headless run ends on cancel (Ctrl+C, docker stop) even while stdin stays open and blocks.</summary>
public sealed class HeadlessRunnerTests
{
    private const int WAIT_SECONDS = 5;

    /// <summary>Like Console.In: ReadLineAsync reads synchronously and blocks until a line comes.</summary>
    private sealed class BlockingReader : TextReader
    {
        private readonly ManualResetEventSlim _release = new ManualResetEventSlim(false);

        public override string? ReadLine()
        {
            _release.Wait();
            return null;
        }

        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            return new ValueTask<string?>(ReadLine());
        }

        public void release()
        {
            _release.Set();
        }
    }

    [Fact]
    public async Task RunAsync_CancelWhileStdinBlocked_Returns()
    {
        // Arrange
        FakeDashboardSource source = new FakeDashboardSource(new DashboardState());
        LogBuffer logBuffer = new LogBuffer(50);
        BlockingReader input = new BlockingReader();
        CancellationTokenSource shutdown = new CancellationTokenSource();
        Task runTask = HeadlessRunner.runAsync(source, logBuffer, new StringWriter(), input, TimeProvider.System, shutdown);

        // Act
        shutdown.Cancel();
        Task finished = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(WAIT_SECONDS)));
        input.release(); // frees the blocked read so the test process ends clean

        // Assert
        Assert.Same(runTask, finished);
    }
}
