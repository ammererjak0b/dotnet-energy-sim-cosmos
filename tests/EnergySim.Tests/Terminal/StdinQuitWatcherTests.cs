using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

public class StdinQuitWatcherTests
{
    [Fact]
    public async Task WaitForQuit_QuitLine_ReturnsTrue()
    {
        StdinQuitWatcher watcher = new StdinQuitWatcher(new StringReader("hello\nQUIT\n"));

        bool quit = await watcher.waitForQuitAsync(CancellationToken.None);

        Assert.True(quit);
    }

    [Fact]
    public async Task WaitForQuit_EndOfInput_ReturnsTrue()
    {
        StdinQuitWatcher watcher = new StdinQuitWatcher(new StringReader("hello\n"));

        bool quit = await watcher.waitForQuitAsync(CancellationToken.None);

        Assert.True(quit);
    }

    [Fact]
    public async Task WaitForQuit_Cancelled_ReturnsFalse()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        StdinQuitWatcher watcher = new StdinQuitWatcher(new StringReader("hello\nquit\n"));

        bool quit = await watcher.waitForQuitAsync(source.Token);

        Assert.False(quit);
    }
}
