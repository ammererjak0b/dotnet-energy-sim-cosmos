using EnergySim.Core.Engine;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

/// <summary>Deterministic shared-clock timelines.</summary>
public sealed class ScheduledClockTests
{
    [Fact]
    public void ReadAdvance_Frozen_ProducesNoSynchronizedIntervals()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(createDefinition(time));
        ClockCursor cursor = new ClockCursor();
        clock.readAdvance(cursor);
        clock.freeze("lost time service");
        time.Advance(TimeSpan.FromSeconds(1));

        // Act
        IReadOnlyList<ClockSnapshot> snapshots = clock.readAdvance(cursor);

        // Assert
        Assert.Single(snapshots);
        Assert.False(snapshots[0].IsSynced);
    }

    [Fact]
    public void Receive_ExplicitRestart_RecoversOnlyAtEffectiveInstant()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(createDefinition(time));
        clock.getSnapshot();
        clock.freeze("missed command");
        ClockDefinition restart = createDefinition(time);
        restart.Revision = 5;
        restart.Generation = 2;
        restart.VirtualAnchor = DateTimeOffset.UnixEpoch.AddYears(1);
        restart.AnchorTimestamp += time.TimestampFrequency / 10;

        // Act
        clock.receive(restart, true);
        bool before = clock.getSnapshot().IsSynced;
        time.Advance(TimeSpan.FromMilliseconds(100));
        ClockSnapshot after = clock.getSnapshot();

        // Assert
        Assert.False(before);
        Assert.True(after.IsSynced);
        Assert.Equal(2, after.Generation);
        Assert.Equal(restart.VirtualAnchor, after.VirtualNow);
    }

    [Fact]
    public void GetSnapshot_HeartbeatExpired_ContinuesWithLastClockSettings()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = createDefinition(time);
        clock.initialize(definition);
        clock.heartbeat(definition.SessionId, 1, time.GetTimestamp());
        clock.getSnapshot();

        // Act
        time.Advance(TimeSpan.FromSeconds(4));
        ClockSnapshot expired = clock.getSnapshot();
        time.Advance(TimeSpan.FromSeconds(1));
        ClockSnapshot later = clock.getSnapshot();

        // Assert
        Assert.False(expired.IsSynced);
        Assert.False(later.IsSynced);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(240), expired.VirtualNow);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(300), later.VirtualNow);
        Assert.Equal(ClockStatus.Running, later.Status);
        Assert.Equal(definition.Speed, later.Speed);
        Assert.Equal(definition.Generation, later.Generation);
        Assert.NotEmpty(later.SyncError);
    }

    [Fact]
    public void GetSnapshot_HeartbeatReturns_ContinuesWithoutTimeJumpOrWarning()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = createDefinition(time);
        clock.initialize(definition);
        clock.heartbeat(definition.SessionId, definition.Revision, time.GetTimestamp());
        time.Advance(TimeSpan.FromSeconds(4));
        clock.getSnapshot();

        // Act
        clock.heartbeat(definition.SessionId, definition.Revision, time.GetTimestamp());
        ClockSnapshot restored = clock.getSnapshot();

        // Assert
        Assert.True(restored.IsSynced);
        Assert.Empty(restored.SyncError);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(240), restored.VirtualNow);
    }

    [Fact]
    public void GetSnapshot_HeartbeatExpiresWhilePaused_KeepsPausedTimeAndWarns()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = createDefinition(time);
        definition.Status = ClockStatus.Paused;
        clock.initialize(definition);
        clock.heartbeat(definition.SessionId, definition.Revision, time.GetTimestamp());
        time.Advance(TimeSpan.FromSeconds(4));

        // Act
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.False(snapshot.IsSynced);
        Assert.Equal(ClockStatus.Paused, snapshot.Status);
        Assert.Equal(definition.VirtualAnchor, snapshot.VirtualNow);
        Assert.Equal(definition.Speed, snapshot.Speed);
        Assert.NotEmpty(snapshot.SyncError);
    }

    [Fact]
    public void Initialize_MonitoringEnabledWithoutFirstHeartbeat_EntersFallbackAfterExpiry()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(createDefinition(time), true);

        // Act
        time.Advance(TimeSpan.FromSeconds(4));
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.False(snapshot.IsSynced);
        Assert.True(snapshot.IsUsingFallback);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(240), snapshot.VirtualNow);
    }

    [Fact]
    public void Initialize_ServiceSideClockWithoutMonitoring_DoesNotEnterFallback()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(createDefinition(time));

        // Act
        time.Advance(TimeSpan.FromSeconds(10));
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.True(snapshot.IsSynced);
        Assert.False(snapshot.IsUsingFallback);
    }

    [Fact]
    public void Heartbeat_PreviousServiceSession_DoesNotKeepNewConnectionAlive()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = createDefinition(time);
        Guid previousSessionId = definition.SessionId;
        clock.initialize(definition, true);
        definition.SessionId = new Guid("22222222-2222-2222-2222-222222222222");
        clock.initialize(definition, true);
        time.Advance(TimeSpan.FromSeconds(4));

        // Act
        clock.heartbeat(previousSessionId, definition.Revision, time.GetTimestamp());
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.False(snapshot.IsSynced);
        Assert.True(snapshot.IsUsingFallback);
        Assert.Equal(definition.SessionId, snapshot.SessionId);
    }

    [Fact]
    public void Receive_AfterServiceRestart_AppliesNewServiceCommands()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition initial = createDefinition(time);
        initial.Revision = 10;
        clock.initialize(initial, true);
        time.Advance(TimeSpan.FromSeconds(4));
        clock.getSnapshot();
        ClockDefinition replacement = createDefinition(time);
        replacement.SessionId = new Guid("22222222-2222-2222-2222-222222222222");
        replacement.Status = ClockStatus.Stopped;
        replacement.VirtualAnchor = DateTimeOffset.UnixEpoch.AddDays(1);
        clock.initialize(replacement, true);
        ClockDefinition start = replacement.copy();
        start.Revision++;
        start.AnchorTimestamp += time.TimestampFrequency / 10;
        start.Status = ClockStatus.Running;

        // Act
        clock.receive(start);
        time.Advance(TimeSpan.FromMilliseconds(200));
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.True(snapshot.IsSynced);
        Assert.Equal(replacement.SessionId, snapshot.SessionId);
        Assert.Equal(2, snapshot.Revision);
        Assert.Equal(ClockStatus.Running, snapshot.Status);
        Assert.Equal(replacement.VirtualAnchor.AddSeconds(6), snapshot.VirtualNow);
    }

    [Theory]
    [InlineData(2, -1)]
    [InlineData(3, 100)]
    public void Receive_LateOrMissingRevision_FreezesClock(int revision, int delayMilliseconds)
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(createDefinition(time));
        DateTimeOffset before = clock.getSnapshot().VirtualNow;
        ClockDefinition change = createDefinition(time);
        change.Revision = revision;
        change.AnchorTimestamp += time.TimestampFrequency * delayMilliseconds / 1000;

        // Act
        clock.receive(change);
        time.Advance(TimeSpan.FromSeconds(1));

        // Assert
        Assert.False(clock.getSnapshot().IsSynced);
        Assert.Equal(before, clock.getSnapshot().VirtualNow);
        Assert.NotEmpty(clock.getSnapshot().SyncError);
    }

    [Fact]
    public void ReadAdvance_PauseBetweenTicks_ContainsFinalRunningInterval()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition initial = createDefinition(time);
        clock.initialize(initial);
        ClockCursor cursor = new ClockCursor();
        clock.readAdvance(cursor);
        ClockDefinition pause = createDefinition(time);
        pause.Revision = 2;
        pause.AnchorTimestamp += time.TimestampFrequency / 10;
        pause.VirtualAnchor = initial.VirtualAnchor.AddSeconds(6);
        pause.Status = ClockStatus.Paused;
        clock.receive(pause);

        // Act
        time.Advance(TimeSpan.FromMilliseconds(200));
        IReadOnlyList<ClockSnapshot> snapshots = clock.readAdvance(cursor);

        // Assert
        Assert.Equal(ClockStatus.Running, snapshots[0].Status);
        Assert.Equal(initial.VirtualAnchor.AddSeconds(6), snapshots[0].VirtualNow);
        Assert.Equal(ClockStatus.Paused, snapshots[snapshots.Count - 1].Status);
        Assert.Equal(initial.VirtualAnchor.AddSeconds(6), clock.getSnapshot().VirtualNow);
    }

    private static ClockDefinition createDefinition(FakeTimeProvider time)
    {
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11111111-1111-1111-1111-111111111111");
        definition.CounterFrequency = time.TimestampFrequency;
        definition.AnchorTimestamp = time.GetTimestamp();
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.Status = ClockStatus.Running;
        return definition;
    }
}
