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

    private static TimeAuthority createAuthorityInState(FakeTimeProvider time, ClockAction[] actions)
    {
        TimeAuthority authority = new TimeAuthority(time, DateTimeOffset.UnixEpoch, 60, TimeSpan.FromMilliseconds(100));
        foreach (ClockAction action in actions)
        {
            authority.execute(action, 60, DateTimeOffset.UnixEpoch);
            time.Advance(TimeSpan.FromMilliseconds(150)); // past the lead time, change is active
        }

        return authority;
    }

    [Fact]
    public void Execute_StepWhilePaused_AdvancesOneTickTimesSpeedAndStaysPaused()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start, ClockAction.Pause });
        DateTimeOffset pausedAt = authority.Clock.getSnapshot().VirtualNow;

        // Act
        ClockDefinition change = authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));

        // Assert
        TimeSpan expectedJump = TimeSpan.FromMilliseconds(SimulationOptions.DEFAULT_TICK_MILLISECONDS * 60); // 200 ms real x 60 = 12 s
        Assert.Equal(ClockStatus.Paused, change.Status);
        Assert.Equal(1, change.Generation);
        Assert.Equal(pausedAt + expectedJump, change.VirtualAnchor);
        Assert.Equal(pausedAt + expectedJump, authority.Clock.getSnapshot().VirtualNow);
    }

    [Fact]
    public void Execute_StepTwice_AdvancesTwoTicks()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start, ClockAction.Pause });
        DateTimeOffset pausedAt = authority.Clock.getSnapshot().VirtualNow;

        // Act
        authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));
        authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));

        // Assert
        Assert.Equal(pausedAt + TimeSpan.FromSeconds(24), authority.Clock.getSnapshot().VirtualNow);
        Assert.Equal(ClockStatus.Paused, authority.Clock.getSnapshot().Status);
    }

    [Fact]
    public void Execute_StepAtDoubleSpeed_JumpsDoubleVirtualTime()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start });
        authority.execute(ClockAction.SetSpeed, 120, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));
        authority.execute(ClockAction.Pause, 120, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));
        DateTimeOffset pausedAt = authority.Clock.getSnapshot().VirtualNow;

        // Act
        authority.execute(ClockAction.Step, 120, DateTimeOffset.UnixEpoch);
        time.Advance(TimeSpan.FromMilliseconds(150));

        // Assert
        Assert.Equal(pausedAt + TimeSpan.FromSeconds(24), authority.Clock.getSnapshot().VirtualNow); // 200 ms x 120
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Execute_StepWhileStoppedOrRunning_Rejects(bool isRunning)
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ClockAction[] actions = new ClockAction[0];
        if (isRunning)
        {
            actions = new ClockAction[] { ClockAction.Start };
        }

        TimeAuthority authority = createAuthorityInState(time, actions);

        // Act
        Action execute = delegate { authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch); };

        // Assert
        Assert.Throws<InvalidOperationException>(execute);
    }

    [Fact]
    public void Execute_StepWhileChangePending_Rejects()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start, ClockAction.Pause });
        authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch); // not yet active

        // Act
        Action execute = delegate { authority.execute(ClockAction.Step, 60, DateTimeOffset.UnixEpoch); };

        // Assert
        Assert.Throws<InvalidOperationException>(execute);
    }

    [Fact]
    public void Execute_PauseThenResume_KeepsVirtualTimeContinuous()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start, ClockAction.Pause });
        DateTimeOffset pausedAt = authority.Clock.getSnapshot().VirtualNow;
        time.Advance(TimeSpan.FromSeconds(30)); // long real pause

        // Act
        ClockDefinition change = authority.execute(ClockAction.Resume, 60, DateTimeOffset.UnixEpoch);

        // Assert
        Assert.Equal(pausedAt, change.VirtualAnchor);
        Assert.Equal(ClockStatus.Running, change.Status);
    }

    [Fact]
    public void Execute_Start_FromRunning_Rejects()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createAuthorityInState(time, new ClockAction[] { ClockAction.Start });

        // Act
        Action execute = delegate { authority.execute(ClockAction.Start, 60, DateTimeOffset.UnixEpoch); };

        // Assert
        Assert.Throws<InvalidOperationException>(execute);
    }
}
