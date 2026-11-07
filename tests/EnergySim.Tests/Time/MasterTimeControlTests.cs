using EnergySim.Core.Engine;
using EnergySim.Time;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Time;

/// <summary>Master controls seen through the clock: pause, resume, speed, restart, monotonic time, common switch instant.</summary>
public sealed class MasterTimeControlTests
{
    private const int LEAD_MILLISECONDS = 100;
    private const double START_SPEED = 60.0;
    private const double SKEW_TOLERANCE_MILLISECONDS = 50.0; // docs/contracts/time.md, speed up to 600

    private static readonly DateTimeOffset START_TIME = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan LEAD = TimeSpan.FromMilliseconds(LEAD_MILLISECONDS);

    private static TimeAuthority createRunningAuthority(FakeTimeProvider time)
    {
        TimeAuthority authority = new TimeAuthority(time, START_TIME, START_SPEED, LEAD);
        authority.execute(ClockAction.Start, START_SPEED, START_TIME);
        time.Advance(LEAD);
        return authority;
    }

    [Fact]
    public void Execute_Pause_FreezesVirtualTimeAfterEffectiveInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        time.Advance(TimeSpan.FromSeconds(2));
        ClockDefinition pause = authority.execute(ClockAction.Pause, START_SPEED, START_TIME);

        // Act
        time.Advance(LEAD);
        ClockSnapshot atInstant = authority.Clock.getSnapshot();
        time.Advance(TimeSpan.FromSeconds(30));
        ClockSnapshot later = authority.Clock.getSnapshot();

        // Assert
        Assert.Equal(ClockStatus.Paused, later.Status);
        Assert.Equal(pause.VirtualAnchor, atInstant.VirtualNow);
        Assert.Equal(atInstant.VirtualNow, later.VirtualNow);
    }

    [Fact]
    public void Execute_Resume_ContinuesWithoutJump()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        authority.execute(ClockAction.Pause, START_SPEED, START_TIME);
        time.Advance(LEAD);
        DateTimeOffset frozen = authority.Clock.getSnapshot().VirtualNow;
        time.Advance(TimeSpan.FromSeconds(10)); // long pause
        ClockDefinition resume = authority.execute(ClockAction.Resume, START_SPEED, START_TIME);

        // Act
        time.Advance(LEAD);
        ClockSnapshot atInstant = authority.Clock.getSnapshot();
        time.Advance(TimeSpan.FromSeconds(1));
        ClockSnapshot oneSecondLater = authority.Clock.getSnapshot();

        // Assert
        Assert.Equal(frozen, resume.VirtualAnchor);
        Assert.Equal(frozen, atInstant.VirtualNow);
        Assert.Equal(ClockStatus.Running, oneSecondLater.Status);
        Assert.Equal(frozen.AddSeconds(START_SPEED), oneSecondLater.VirtualNow);
    }

    [Fact]
    public void Execute_SetSpeed_IsContinuousAtEffectiveInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        ClockDefinition change = authority.execute(ClockAction.SetSpeed, 600.0, START_TIME);

        // Act
        DateTimeOffset justBefore = authority.virtualNowAt(change.AnchorTimestamp - 1);
        DateTimeOffset atInstant = authority.virtualNowAt(change.AnchorTimestamp);
        DateTimeOffset oneSecondAfter = authority.virtualNowAt(change.AnchorTimestamp + time.TimestampFrequency);

        // Assert
        Assert.True((atInstant - justBefore).Duration() < TimeSpan.FromMilliseconds(1));
        Assert.Equal(TimeSpan.FromSeconds(600), oneSecondAfter - atInstant);
    }

    [Fact]
    public void Execute_Restart_SetsNewTimeAndIncrementsGenerationAtEffectiveInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        DateTimeOffset restartTime = new DateTimeOffset(2030, 5, 6, 7, 8, 0, TimeSpan.Zero);
        authority.execute(ClockAction.Restart, START_SPEED, restartTime);

        // Act
        ClockSnapshot before = authority.Clock.getSnapshot();
        time.Advance(LEAD);
        ClockSnapshot atInstant = authority.Clock.getSnapshot();

        // Assert
        Assert.Equal(1, before.Generation);
        Assert.Equal(2, atInstant.Generation);
        Assert.Equal(restartTime, atInstant.VirtualNow);
        Assert.Equal(ClockStatus.Running, atInstant.Status);
    }

    [Fact]
    public void GetSnapshot_PauseResumeAndSpeedChanges_IsMonotonicWithinGeneration()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        ClockAction[] actions = { ClockAction.SetSpeed, ClockAction.Pause, ClockAction.Resume, ClockAction.SetSpeed, ClockAction.Pause, ClockAction.Resume };
        double[] speeds = { 600.0, 600.0, 600.0, 10.0, 10.0, 10.0 };
        DateTimeOffset previous = authority.Clock.getSnapshot().VirtualNow;
        bool wentBackwards = false;

        // Act
        for (int index = 0; index < actions.Length; index++)
        {
            authority.execute(actions[index], speeds[index], START_TIME);
            for (int step = 0; step < 30; step++)
            {
                time.Advance(TimeSpan.FromMilliseconds(10));
                DateTimeOffset now = authority.Clock.getSnapshot().VirtualNow;
                if (now < previous)
                {
                    wentBackwards = true;
                }

                previous = now;
            }
        }

        // Assert
        Assert.False(wentBackwards);
        Assert.Equal(1, authority.Clock.getSnapshot().Generation);
    }

    [Fact]
    public void ReadAdvance_ClocksWithDifferentPollTimes_SwitchAtSameMasterInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        ScheduledClock fastPoller = createParticipant(time, authority);
        ScheduledClock slowPoller = createParticipant(time, authority);
        ClockCursor fastCursor = new ClockCursor();
        ClockCursor slowCursor = new ClockCursor();
        fastPoller.readAdvance(fastCursor);
        slowPoller.readAdvance(slowCursor);
        ClockDefinition change = authority.execute(ClockAction.SetSpeed, 600.0, START_TIME);
        fastPoller.receive(change);
        slowPoller.receive(change);
        List<ClockSnapshot> fastSnapshots = new List<ClockSnapshot>();
        List<ClockSnapshot> slowSnapshots = new List<ClockSnapshot>();

        // Act
        for (int step = 1; step <= 60; step++)
        {
            time.Advance(TimeSpan.FromMilliseconds(10));
            if (step % 3 == 0)
            {
                fastSnapshots.AddRange(fastPoller.readAdvance(fastCursor)); // 30 ms poll
            }

            if (step % 7 == 0)
            {
                slowSnapshots.AddRange(slowPoller.readAdvance(slowCursor)); // 70 ms poll
            }
        }

        // Assert
        ClockSnapshot fastSwitch = findFirstWithSpeed(fastSnapshots, 600.0);
        ClockSnapshot slowSwitch = findFirstWithSpeed(slowSnapshots, 600.0);
        Assert.Equal(change.VirtualAnchor, fastSwitch.VirtualNow);
        Assert.Equal(fastSwitch.VirtualNow, slowSwitch.VirtualNow);
    }

    [Fact]
    public void GetSnapshot_ManyParticipantsRepeatedSpeedChanges_StaysWithinSkewTolerance()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        TimeAuthority authority = createRunningAuthority(time);
        int[] deliveryDelaysMilliseconds = { 0, 10, 25, 40, 60, 90 }; // all inside the lead time
        int[] pollMilliseconds = { 5, 10, 15, 20, 35, 50 };
        List<ScheduledClock> participants = new List<ScheduledClock>();
        foreach (int unused in deliveryDelaysMilliseconds)
        {
            participants.Add(createParticipant(time, authority));
        }

        double[] speeds = { 600.0, 60.0, 300.0, 3.0, 600.0, 120.0, 1.0, 600.0, 60.0, 600.0 };
        double maxSkewMilliseconds = 0.0;

        // Act
        foreach (double speed in speeds)
        {
            ClockDefinition change = authority.execute(ClockAction.SetSpeed, speed, START_TIME);
            for (int elapsedMilliseconds = 0; elapsedMilliseconds < 300; elapsedMilliseconds += 5)
            {
                for (int index = 0; index < participants.Count; index++)
                {
                    if (elapsedMilliseconds == deliveryDelaysMilliseconds[index])
                    {
                        participants[index].receive(change);
                    }

                    if (elapsedMilliseconds % pollMilliseconds[index] == 0)
                    {
                        DateTimeOffset masterNow = authority.virtualNowAt(time.GetTimestamp());
                        double skewMilliseconds = (participants[index].getSnapshot().VirtualNow - masterNow).Duration().TotalMilliseconds;
                        maxSkewMilliseconds = Math.Max(maxSkewMilliseconds, skewMilliseconds);
                    }
                }

                time.Advance(TimeSpan.FromMilliseconds(5));
            }
        }

        // Assert
        Assert.True(maxSkewMilliseconds <= SKEW_TOLERANCE_MILLISECONDS, "max skew " + maxSkewMilliseconds + " ms");
    }

    private static ScheduledClock createParticipant(FakeTimeProvider time, TimeAuthority authority)
    {
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(authority.current());
        return clock;
    }

    private static ClockSnapshot findFirstWithSpeed(List<ClockSnapshot> snapshots, double speed)
    {
        foreach (ClockSnapshot snapshot in snapshots)
        {
            if (snapshot.Speed == speed)
            {
                return snapshot;
            }
        }

        throw new InvalidOperationException("no snapshot with speed " + speed);
    }
}
