using EnergySim.Core.Engine;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

public class LocalClockTests
{
    private static readonly DateTimeOffset START = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);

    private static LocalClock createClock(FakeTimeProvider timeProvider, double speed)
    {
        return new LocalClock(timeProvider, START, speed);
    }

    [Fact]
    public void GetSnapshot_AfterCreate_IsRunningAtStartTimeAndSynced()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);

        // Act
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.Equal(START, snapshot.VirtualNow);
        Assert.Equal(ClockStatus.Running, snapshot.Status);
        Assert.Equal(60.0, snapshot.Speed);
        Assert.True(snapshot.IsSynced);
    }

    [Fact]
    public void GetSnapshot_WhileRunning_VirtualTimeIsRealTimeTimesSpeed()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);

        // Act
        timeProvider.Advance(TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(START.AddMinutes(10), clock.getSnapshot().VirtualNow);
    }

    [Fact]
    public void Pause_FreezesVirtualTimeWithoutJump()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        // Act
        clock.pause();
        timeProvider.Advance(TimeSpan.FromSeconds(100));

        // Assert
        ClockSnapshot snapshot = clock.getSnapshot();
        Assert.Equal(ClockStatus.Paused, snapshot.Status);
        Assert.Equal(START.AddMinutes(5), snapshot.VirtualNow);
    }

    [Fact]
    public void Resume_ContinuesFromPausedTimeWithoutJump()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        clock.pause();
        timeProvider.Advance(TimeSpan.FromSeconds(100));

        // Act
        clock.resume();
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal(ClockStatus.Running, clock.getSnapshot().Status);
        Assert.Equal(START.AddMinutes(7), clock.getSnapshot().VirtualNow);
    }

    [Fact]
    public void SetSpeed_KeepsVirtualTimeAndAppliesNewSpeedFromNow()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);
        timeProvider.Advance(TimeSpan.FromSeconds(10));

        // Act
        clock.setSpeed(3600.0);
        DateTimeOffset directlyAfter = clock.getSnapshot().VirtualNow;
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(START.AddMinutes(10), directlyAfter); // no jump
        Assert.Equal(START.AddMinutes(10).AddHours(1), clock.getSnapshot().VirtualNow);
        Assert.Equal(3600.0, clock.getSnapshot().Speed);
    }

    [Fact]
    public void SetSpeed_WhilePaused_StaysPaused()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);
        clock.pause();

        // Act
        clock.setSpeed(10.0);
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(START, clock.getSnapshot().VirtualNow);
        Assert.Equal(10.0, clock.getSnapshot().Speed);
    }

    [Fact]
    public void SetTime_JumpsAndStartsNewGeneration()
    {
        // Arrange
        FakeTimeProvider timeProvider = new FakeTimeProvider();
        LocalClock clock = createClock(timeProvider, 60.0);
        int generationBefore = clock.getSnapshot().Generation;
        DateTimeOffset newTime = new DateTimeOffset(2026, 12, 24, 12, 0, 0, TimeSpan.Zero);

        // Act
        clock.setTime(newTime);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(newTime.AddMinutes(1), clock.getSnapshot().VirtualNow);
        Assert.Equal(generationBefore + 1, clock.getSnapshot().Generation);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(3601.0)]
    public void SetSpeed_OutOfRange_Throws(double speed)
    {
        // Arrange
        LocalClock clock = createClock(new FakeTimeProvider(), 60.0);

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(delegate { clock.setSpeed(speed); });
    }

    [Fact]
    public void Constructor_SpeedOutOfRange_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(delegate { createClock(new FakeTimeProvider(), 0.0); });
    }
}
