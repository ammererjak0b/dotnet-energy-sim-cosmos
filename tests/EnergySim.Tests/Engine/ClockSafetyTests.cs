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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Initialize_IncompatibleOrRepeatedInitialization_DoesNotSilentlyReplaceClock(bool wrongFrequency)
    {
        // Arrange
        FakeTimeProvider time = new FakeTimeProvider();
        ScheduledClock clock = new ScheduledClock(time);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = time.TimestampFrequency;
        clock.initialize(definition);
        if (wrongFrequency)
        {
            definition.CounterFrequency++;
        }
        else
        {
            definition.SessionId = Guid.NewGuid();
        }

        // Act
        clock.initialize(definition);

        // Assert
        Assert.False(clock.getSnapshot().IsSynced);
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
