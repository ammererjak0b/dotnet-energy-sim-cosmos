using EnergySim.Core.Engine;
using EnergySim.Time;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

/// <summary>Authority schedules continuous changes against an injected counter.</summary>
public sealed class TimeAuthorityTests
{
    [Theory]
    [InlineData(false, ClockAction.Pause)]
    [InlineData(false, ClockAction.Resume)]
    [InlineData(true, ClockAction.SetSpeed)]
    public void Execute_InvalidStateOrPendingCommand_RejectsChange(bool pending, ClockAction action)
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = new TimeAuthority(time, DateTimeOffset.UnixEpoch, 60, TimeSpan.FromMilliseconds(100));
        if (pending)
        {
            authority.execute(ClockAction.Start, 60, DateTimeOffset.UnixEpoch);
        }

        // Act
        Action execute = delegate { authority.execute(action, 120, DateTimeOffset.UnixEpoch); };

        // Assert
        Assert.Throws<InvalidOperationException>(execute);
    }

    [Theory]
    [InlineData(ClockAction.Pause, ClockStatus.Paused, 60, 1)]
    [InlineData(ClockAction.SetSpeed, ClockStatus.Running, 120, 1)]
    [InlineData(ClockAction.Restart, ClockStatus.Running, 60, 2)]
    public void Execute_Control_PreservesTimeExceptExplicitRestart(ClockAction action, ClockStatus state, double expectedSpeed, int generation)
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = new TimeAuthority(time, DateTimeOffset.UnixEpoch, 60, TimeSpan.FromMilliseconds(100));
        authority.execute(ClockAction.Start, 60, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(200));
        DateTimeOffset restartTime = DateTimeOffset.UnixEpoch.AddYears(1);

        // Act
        ClockDefinition change = authority.execute(action, 120, restartTime);

        // Assert
        Assert.Equal(state, change.Status);
        Assert.Equal(expectedSpeed, change.Speed);
        Assert.Equal(generation, change.Generation);
        DateTimeOffset expected = DateTimeOffset.UnixEpoch.AddSeconds(12);
        if (action == ClockAction.Restart)
        {
            expected = restartTime;
        }

        Assert.Equal(expected, change.VirtualAnchor);
    }

    [Fact]
    public void Execute_Start_RemainsStoppedUntilScheduledInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = new TimeAuthority(time, DateTimeOffset.UnixEpoch, 60, TimeSpan.FromMilliseconds(100));

        // Act
        ClockDefinition change = authority.execute(ClockAction.Start, 60, DateTimeOffset.UnixEpoch);
        ClockSnapshot before = authority.Clock.getSnapshot();
        time.Advance(TimeSpan.FromMilliseconds(150));

        // Assert
        Assert.Equal(ClockStatus.Stopped, before.Status);
        Assert.Equal(2, change.Revision);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(3), authority.Clock.getSnapshot().VirtualNow);
    }
}
