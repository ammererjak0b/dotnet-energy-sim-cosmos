using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

/// <summary>Clock boundaries survive the slower simulation polling interval.</summary>
public sealed class SharedClockLoopTests
{
    [Theory]
    [InlineData(1, 60.0)]
    [InlineData(3, 120.0)]
    public async Task RunAsync_SameSessionReconnect_PreservesDeviceProgress(int revision, double speed)
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        ScheduledClock clock = new ScheduledClock(harness.TimeProvider);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11111111-1111-1111-1111-111111111111");
        definition.CounterFrequency = harness.TimeProvider.TimestampFrequency;
        definition.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.Status = ClockStatus.Running;
        clock.initialize(definition, true);
        harness.ClockSource = clock;
        await harness.startAsync();
        await harness.advanceRealTimeAsync(TimeSpan.FromSeconds(4));
        definition.VirtualAnchor = clock.getSnapshot().VirtualNow;
        definition.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        definition.Revision = revision;
        definition.Speed = speed;

        // Act
        clock.initialize(definition, true);
        await harness.advanceRealTimeAsync(harness.Tick);
        await harness.stopAsync();

        // Assert
        Assert.Single(harness.Device.getResets());
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(harness.Tick * speed, updates[updates.Count - 1].Step);
        Assert.True(harness.Sink.LastFrame.Snapshot.IsSynced);
        Assert.Empty(harness.Sink.LastFrame.Snapshot.SyncError);
    }

    [Fact]
    public async Task RunAsync_NewServiceSessionWithSameGeneration_ResetsWithoutIntegratingTimeJump()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        ScheduledClock clock = new ScheduledClock(harness.TimeProvider);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11111111-1111-1111-1111-111111111111");
        definition.CounterFrequency = harness.TimeProvider.TimestampFrequency;
        definition.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.Status = ClockStatus.Running;
        clock.initialize(definition);
        harness.ClockSource = clock;
        await harness.startAsync();
        await harness.advanceRealTimeAsync(harness.Tick);
        definition.SessionId = new Guid("22222222-2222-2222-2222-222222222222");
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch.AddDays(1);
        definition.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        definition.Status = ClockStatus.Stopped;

        // Act
        clock.initialize(definition);
        await harness.advanceRealTimeAsync(harness.Tick);
        await harness.stopAsync();

        // Assert
        List<DateTimeOffset> resets = harness.Device.getResets();
        Assert.Equal(2, resets.Count);
        Assert.Equal(definition.VirtualAnchor, resets[1]);
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(TimeSpan.Zero, updates[updates.Count - 1].Step);
        Assert.True(harness.Sink.LastFrame.Snapshot.IsSynced);
        Assert.Equal(ClockStatus.Stopped, harness.Sink.LastFrame.Snapshot.Status);
    }

    [Fact]
    public async Task RunAsync_HeartbeatsStop_KeepsUpdatingDeviceAndApplyingCommands()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        ScheduledClock clock = new ScheduledClock(harness.TimeProvider);
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11111111-1111-1111-1111-111111111111");
        definition.CounterFrequency = harness.TimeProvider.TimestampFrequency;
        definition.AnchorTimestamp = harness.TimeProvider.GetTimestamp();
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.Status = ClockStatus.Running;
        clock.initialize(definition);
        clock.heartbeat(definition.SessionId, definition.Revision, harness.TimeProvider.GetTimestamp());
        harness.ClockSource = clock;
        await harness.startAsync();
        harness.Commands.Writer.TryWrite(new EnergySim.Core.Devices.DeviceCommand("TurnOff", 0));

        // Act
        await harness.advanceRealTimeAsync(TimeSpan.FromSeconds(4));
        await harness.stopAsync();

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(2, updates.Count);
        Assert.Equal(TimeSpan.FromSeconds(240), updates[1].Step);
        Assert.Single(harness.Device.getResets());
        Assert.Single(harness.Device.getCommands());
        Assert.False(harness.Sink.LastFrame.Snapshot.IsSynced);
        Assert.NotEmpty(harness.Sink.LastFrame.Snapshot.SyncError);
    }

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
