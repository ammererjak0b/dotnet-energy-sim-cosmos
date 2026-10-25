using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Terminal;
using EnergySim.Time;
using EnergySim.Time.Cli;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

/// <summary>Time service reset accepts an explicitly entered local timestamp.</summary>
public sealed class TimeServiceDashboardSourceTests
{
    private const string TIME_FORMAT = "yyyy-MM-dd HH:mm";

    [Fact]
    public void GetState_ResetCommand_RequestsTimestampText()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeServiceDashboardSource source = createSource(createAuthority(time));

        // Act
        DashboardState state = source.getState();
        CommandDescriptor command = findResetCommand(state);

        // Assert
        Assert.True(command.HasParameter);
        Assert.True(command.IsTextParameter);
        Assert.Equal(TIME_FORMAT, command.Unit);
    }

    [Fact]
    public async Task ExecuteAsync_ResetWithValidTimestamp_SchedulesRestartAtEnteredTime()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthority(time);
        TimeServiceDashboardSource source = createSource(authority);
        DeviceCommand command = new DeviceCommand(TimeServiceDashboardSource.COMMAND_RESTART, "2030-02-03 04:05");

        // Act
        CommandResult result = await source.executeAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(new DateTimeOffset(2030, 2, 3, 4, 5, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2030, 2, 3, 4, 5, 0))),
            authority.pending()!.VirtualAnchor);
    }

    [Fact]
    public async Task ExecuteAsync_ResetWithInvalidTimestamp_RejectsInput()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeServiceDashboardSource source = createSource(createAuthority(time));
        DeviceCommand command = new DeviceCommand(TimeServiceDashboardSource.COMMAND_RESTART, "03/02/2030 04:05");

        // Act
        CommandResult result = await source.executeAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result.Accepted);
        Assert.Contains(TIME_FORMAT, result.Message);
    }

    private static TimeAuthority createAuthority(FakeTimeProvider time)
    {
        DateTimeOffset start = DateTimeOffset.UnixEpoch;
        return new TimeAuthority(time, start, 60, TimeSpan.FromMilliseconds(100));
    }

    private static TimeServiceDashboardSource createSource(TimeAuthority authority)
    {
        DateTimeOffset start = DateTimeOffset.UnixEpoch;
        TimeServiceOptions options = new TimeServiceOptions();
        ClockServiceHost host = new ClockServiceHost(authority, TimeProvider.System, options);
        return new TimeServiceDashboardSource(authority, host, start, new LogBuffer(20), delegate { });
    }

    private static CommandDescriptor findResetCommand(DashboardState state)
    {
        foreach (CommandDescriptor command in state.Commands)
        {
            if (command.Name == TimeServiceDashboardSource.COMMAND_RESTART)
            {
                return command;
            }
        }

        throw new InvalidOperationException("Reset command was not found.");
    }
}
