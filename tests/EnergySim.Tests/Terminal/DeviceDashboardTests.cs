using EnergySim.Core.Devices;
using EnergySim.Terminal;
using Spectre.Console;
using Spectre.Console.Testing;

namespace EnergySim.Tests.Terminal;

public class DeviceDashboardTests
{
    private static readonly TimeSpan TEST_REFRESH = TimeSpan.FromMilliseconds(5);

    private static DashboardState createState()
    {
        DashboardState state = new DashboardState();
        state.DeviceId = "bat-1";
        state.DeviceTypeName = "Battery";
        state.LocationId = "salzburg";
        state.ClockText = "2026-06-21 06:00 CEST (04:00Z)";
        state.Speed = 60;
        state.ClockStatus = "Running";
        state.WarningText = "none";
        state.HeaderRows.Add(new DisplayRow { Label = "Power", Value = "1.5 kW" });
        state.DeviceRows.Add(new DisplayRow { Label = "SoC", Value = "75 %", HasBar = true, BarFraction = 0.75 });
        state.LogLines.Add("04:00:01 INF started");
        state.Commands.Add(new CommandDescriptor { Name = CommonCommands.TURN_ON, Key = 'O', Label = "On" });
        state.Commands.Add(new CommandDescriptor
        {
            Name = CommonCommands.SET_LIMIT,
            Key = 'L',
            Label = "Limit",
            HasParameter = true,
            Minimum = 0,
            Maximum = 100,
            Unit = "%"
        });
        state.BannerText = "NO MASTER";
        state.BannerSeverity = DisplaySeverity.Warning;
        return state;
    }

    private static TestConsole createConsole()
    {
        TestConsole console = new TestConsole();
        console.Profile.Width = 100;
        console.Interactive();
        return console;
    }

    private static async Task runWithTimeoutAsync(DeviceDashboard dashboard, FakeDashboardSource source)
    {
        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await dashboard.runAsync(source, cts.Token);
        Assert.False(cts.IsCancellationRequested); // finished by quit, not by timeout
    }

    [Fact]
    public async Task RunAsync_RendersStateAndLegend()
    {
        // Arrange
        TestConsole console = createConsole();
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("q");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(console, keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        string text = console.Output;
        Assert.Contains("Battery", text);
        Assert.Contains("bat-1", text);
        Assert.Contains("salzburg", text);
        Assert.Contains("2026-06-21 06:00 CEST (04:00Z)", text);
        Assert.Contains("60", text);
        Assert.Contains("Running", text);
        Assert.Contains("NO MASTER", text);
        Assert.Contains("Power", text);
        Assert.Contains("SoC", text);
        Assert.Contains("75 %", text);
        Assert.Contains("started", text);
        Assert.Contains("[O] On", text);
        Assert.Contains("[L] Limit", text);
        Assert.Contains("[Q] Quit", text);
    }

    [Fact]
    public async Task RunAsync_QuitKey_CallsRequestShutdownAndReturns()
    {
        // Arrange
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("Q");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(createConsole(), keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Equal(1, source.ShutdownCount);
        Assert.Empty(source.Executed);
    }

    [Fact]
    public async Task RunAsync_CommandKey_ExecutesCommandAndLogsResult()
    {
        // Arrange
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("oq"); // lower case works
        FakeDashboardSource source = new FakeDashboardSource(createState());
        LogBuffer logBuffer = new LogBuffer(10);
        DeviceDashboard dashboard = new DeviceDashboard(createConsole(), keys, TimeProvider.System, logBuffer, TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Single(source.Executed);
        Assert.Equal(CommonCommands.TURN_ON, source.Executed[0].Name);
        Assert.Contains("done TurnOn", logBuffer.getLast(5)[0]);
    }

    [Fact]
    public async Task RunAsync_ParameterCommand_PromptsAndExecutesWithValue()
    {
        // Arrange
        TestConsole console = createConsole();
        console.Input.PushTextWithEnter("42");
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("lq");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(console, keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Single(source.Executed);
        Assert.Equal(CommonCommands.SET_LIMIT, source.Executed[0].Name);
        Assert.Equal(42.0, source.Executed[0].Value);
    }

    [Fact]
    public async Task RunAsync_TextParameterCommand_PromptsAndExecutesWithText()
    {
        // Arrange
        TestConsole console = createConsole();
        console.Input.PushTextWithEnter("2030-02-03 04:05");
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("rq");
        DashboardState state = createState();
        state.Commands.Add(DeviceSupport.createTextParameterCommand("Restart", 'R', "Restart time", "yyyy-MM-dd HH:mm"));
        FakeDashboardSource source = new FakeDashboardSource(state);
        DeviceDashboard dashboard = new DeviceDashboard(console, keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Single(source.Executed);
        Assert.Equal("Restart", source.Executed[0].Name);
        Assert.Equal("2030-02-03 04:05", source.Executed[0].TextValue);
    }

    [Fact]
    public async Task RunAsync_ParameterOutOfRange_IsRejectedUntilValid()
    {
        // Arrange
        TestConsole console = createConsole();
        console.Input.PushTextWithEnter("500"); // too high
        console.Input.PushTextWithEnter("abc"); // not a number
        console.Input.PushTextWithEnter("7,5"); // decimal comma is fine
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("lq");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(console, keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Single(source.Executed);
        Assert.Equal(7.5, source.Executed[0].Value);
        Assert.Contains("between 0 and 100", console.Output);
    }

    [Fact]
    public async Task RunAsync_ParameterPromptEmptyInput_CancelsCommand()
    {
        // Arrange
        TestConsole console = createConsole();
        console.Input.PushTextWithEnter(string.Empty);
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("lq");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(console, keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Empty(source.Executed);
    }

    [Fact]
    public async Task RunAsync_UnknownKey_IsIgnored()
    {
        // Arrange
        FakeKeySource keys = new FakeKeySource();
        keys.enqueue("zq");
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(createConsole(), keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);

        // Act
        await runWithTimeoutAsync(dashboard, source);

        // Assert
        Assert.Empty(source.Executed);
    }

    [Fact]
    public async Task RunAsync_WhenCancelled_ReturnsWithoutThrowing()
    {
        // Arrange
        FakeKeySource keys = new FakeKeySource(); // never a key
        FakeDashboardSource source = new FakeDashboardSource(createState());
        DeviceDashboard dashboard = new DeviceDashboard(createConsole(), keys, TimeProvider.System, new LogBuffer(10), TEST_REFRESH);
        using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        await dashboard.runAsync(source, cts.Token);

        // Assert
        Assert.True(cts.IsCancellationRequested);
        Assert.Equal(0, source.ShutdownCount);
    }
}
