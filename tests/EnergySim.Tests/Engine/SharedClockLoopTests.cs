using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

/// <summary>Clock boundaries survive the slower simulation polling interval.</summary>
public sealed class SharedClockLoopTests
{
    [Fact]
    public async Task RunAsync_PauseBetweenTicks_AdvancesExactlyToPause()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        ScheduledClock clock = new ScheduledClock(harness.TimeProvider);
        ClockDefinition initial = new ClockDefinition();
        initial.SessionId = Guid.NewGuid();
        initial.CounterFrequency = harness.TimeProvider.TimestampFrequency;
        initial.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        initial.VirtualAnchor = DateTimeOffset.UnixEpoch;
        initial.Status = ClockStatus.Running;
        clock.initialize(initial);
        harness.ClockSource = clock;
        await harness.startAsync();
        ClockDefinition pause = new ClockDefinition();
        pause.SessionId = initial.SessionId;
        pause.CounterFrequency = initial.CounterFrequency;
        pause.Revision = 2;
        pause.AnchorTimestamp = initial.AnchorTimestamp + initial.CounterFrequency / 10;
        pause.VirtualAnchor = initial.VirtualAnchor.AddSeconds(6);
        pause.Status = ClockStatus.Paused;
        clock.receive(pause);

        // Act
        await harness.advanceRealTimeAsync(TimeSpan.FromMilliseconds(200));
        await harness.stopAsync();

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(2, updates.Count);
        Assert.Equal(TimeSpan.FromSeconds(6), updates[1].Step);
    }
}
