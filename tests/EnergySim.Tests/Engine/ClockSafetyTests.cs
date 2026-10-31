using EnergySim.Core.Engine;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.Engine;

/// <summary>Rejects incompatible clock epochs and protects installed values.</summary>
public sealed class ClockSafetyTests
{
    [Fact]
    public void Heartbeat_WhenFutureRevisionArrivesBeforeDefinition_DoesNotFaultClock()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = time.TimestampFrequency;
        ScheduledClock clock = new ScheduledClock(time);
        clock.initialize(definition);
        clock.heartbeat(definition.SessionId, 2, time.GetTimestamp());

        // Act
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.True(snapshot.IsSynced);
        Assert.Equal(1, snapshot.Revision);
    }

    [Fact]
    public void Initialize_IncompatibleCounterFrequency_RejectsClock()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = time.TimestampFrequency;
        clock.initialize(definition);
        definition.CounterFrequency++;

        // Act
        clock.initialize(definition);

        // Assert
        Assert.False(clock.getSnapshot().IsSynced);
    }

    [Fact]
    public void Initialize_NewServiceSession_AcceptsItsAuthoritativeClock()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11111111-1111-1111-1111-111111111111");
        definition.CounterFrequency = time.TimestampFrequency;
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.Revision = 5;
        clock.initialize(definition);
        clock.freeze("old service disconnected");
        definition.SessionId = new Guid("22222222-2222-2222-2222-222222222222");
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch.AddDays(1);
        definition.Revision = 1;

        // Act
        clock.initialize(definition);
        ClockSnapshot snapshot = clock.getSnapshot();

        // Assert
        Assert.True(snapshot.IsSynced);
        Assert.Equal(definition.SessionId, snapshot.SessionId);
        Assert.Equal(1, snapshot.Revision);
        Assert.Equal(definition.VirtualAnchor, snapshot.VirtualNow);
        Assert.Empty(snapshot.SyncError);
    }

    [Fact]
    public void Initialize_CallerMutatesDefinition_ClockKeepsInstalledAnchor()
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = time.TimestampFrequency;
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        clock.initialize(definition);

        // Act
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch.AddYears(10);

        // Assert
        Assert.Equal(DateTimeOffset.UnixEpoch, clock.getSnapshot().VirtualNow);
    }
}
