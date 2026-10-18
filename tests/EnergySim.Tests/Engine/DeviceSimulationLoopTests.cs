using EnergySim.Core.Engine;
using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Engine;

public class DeviceSimulationLoopTests
{
    private static TimeSpan sumSteps(List<DeviceUpdateCall> updates)
    {
        TimeSpan sum = TimeSpan.Zero;
        foreach (DeviceUpdateCall update in updates)
        {
            sum = sum + update.Step;
        }

        return sum;
    }

    [Fact]
    public async Task RunAsync_StepIsDifferenceOfClockReadings_NotIntegrated()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        DateTimeOffset start = harness.Clock.VirtualNow;

        // Act
        harness.Clock.VirtualNow = start.AddSeconds(7); // unrelated to tick * speed
        await harness.advanceRealTimeAsync(harness.Tick);
        harness.Clock.VirtualNow = start.AddSeconds(20);
        await harness.advanceRealTimeAsync(harness.Tick);

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(3, updates.Count); // initial + 2 steps
        Assert.Equal(TimeSpan.Zero, updates[0].Step);
        Assert.Equal(TimeSpan.FromSeconds(7), updates[1].Step);
        Assert.Equal(TimeSpan.FromSeconds(13), updates[2].Step);
        Assert.Equal(start.AddSeconds(20), updates[2].VirtualNow);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_SpeedChange_NoTimeJump_DeviceSeesContinuousSteps()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        DateTimeOffset start = harness.Clock.VirtualNow;
        List<double> speeds = new List<double> { 1.0, 1.0, 60.0, 60.0, 3600.0, 0.5, 3600.0 };

        // Act
        foreach (double speed in speeds)
        {
            harness.Clock.Speed = speed;
            await harness.tickAsync();
        }

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(harness.Clock.VirtualNow - start, sumSteps(updates));
        for (int index = 0; index < speeds.Count; index++)
        {
            Assert.Equal(harness.Tick * speeds[index], updates[index + 1].Step); // index 0 = initial update
        }

        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_Pause_ZeroStep_RegistersStillPublished_SequenceConstant()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        await harness.tickAsync();
        int updatesBeforePause = harness.Device.getUpdates().Count;
        harness.Clock.Status = ClockStatus.Paused;
        await harness.tickAsync();
        uint sequenceAtPause = harness.Sink.LastFrame.Sequence;
        int framesAtPause = harness.Sink.FrameCount;

        // Act
        await harness.tickAsync();
        await harness.tickAsync();
        await harness.tickAsync();

        // Assert
        Assert.Equal(framesAtPause + 3, harness.Sink.FrameCount);
        Assert.Equal(sequenceAtPause, harness.Sink.LastFrame.Sequence);
        Assert.Equal(updatesBeforePause, harness.Device.getUpdates().Count);
        Assert.Equal(harness.Device.DisplayRows.Count, harness.Sink.LastFrame.DisplayRows.Count);
        Assert.Equal(ClockStatus.Paused, harness.Sink.LastFrame.Snapshot.Status);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_Resume_ContinuesWithoutJump()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        await harness.tickAsync();
        harness.Clock.Status = ClockStatus.Paused;
        await harness.tickAsync();
        await harness.advanceRealTimeAsync(TimeSpan.FromSeconds(30)); // long real pause
        DateTimeOffset pausedAt = harness.Clock.VirtualNow;

        // Act
        harness.Clock.Status = ClockStatus.Running;
        await harness.tickAsync();

        // Assert
        DeviceUpdateCall last = harness.Device.getUpdates()[harness.Device.getUpdates().Count - 1];
        Assert.Equal(harness.Tick, last.Step);
        Assert.Equal(pausedAt + harness.Tick, last.VirtualNow);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_GenerationChange_ResetsDevice()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        await harness.tickAsync();
        DateTimeOffset restartTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        uint sequenceBefore = harness.Sink.LastFrame.Sequence;

        // Act
        harness.Clock.Generation = 2;
        harness.Clock.VirtualNow = restartTime; // backwards jump
        await harness.advanceRealTimeAsync(harness.Tick);
        await harness.tickAsync();

        // Assert
        List<DateTimeOffset> resets = harness.Device.getResets();
        Assert.Equal(2, resets.Count); // first sync + restart
        Assert.Equal(restartTime, resets[1]);
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(TimeSpan.Zero, updates[updates.Count - 2].Step); // initial update after reset, no jump
        Assert.Equal(harness.Tick, updates[updates.Count - 1].Step);
        Assert.True(harness.Sink.LastFrame.Sequence > sequenceBefore);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_StoppedToRunning_ResetsDeviceAtStartTimeWithoutHugeStep()
    {
        // Arrange: devices synced while the master is stopped (virtual time = epoch)
        LoopHarness harness = new LoopHarness(false);
        harness.Clock.Status = ClockStatus.Stopped;
        harness.Clock.VirtualNow = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await harness.startAsync();
        await harness.tickAsync();
        DateTimeOffset startTime = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);

        // Act: master start jumps the virtual time to the start time and runs
        harness.Clock.VirtualNow = startTime;
        harness.Clock.Status = ClockStatus.Running;
        await harness.advanceRealTimeAsync(harness.Tick);
        await harness.tickAsync();

        // Assert: device restarted at the start time, no step from 1970
        List<DateTimeOffset> resets = harness.Device.getResets();
        Assert.Equal(startTime, resets[resets.Count - 1]);
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(TimeSpan.Zero, updates[updates.Count - 2].Step);
        Assert.Equal(harness.Tick, updates[updates.Count - 1].Step);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_NotSynced_PublishesFrameWithoutUpdating()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(true);
        harness.Clock.IsSynced = false;

        // Act
        await harness.startAsync();
        await harness.tickAsync();

        // Assert
        Assert.False(harness.Sink.LastFrame.Snapshot.IsSynced);
        Assert.NotNull(harness.Sink.LastFrame.Header);
        Assert.Empty(harness.Device.getUpdates());
        Assert.Empty(harness.Device.getResets());
        Assert.Equal(0, harness.Weather.CallCount);

        // Act: sync arrives
        harness.Clock.IsSynced = true;
        await harness.tickAsync();

        // Assert
        Assert.Single(harness.Device.getResets());
        Assert.Single(harness.Device.getUpdates());
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_WeatherStale_SetsFlag()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(true);
        harness.Weather.Result.IsStale = true;
        harness.Weather.Result.ErrorMessage = "connection refused";

        // Act
        await harness.startAsync();

        // Assert
        Assert.True(harness.Sink.LastFrame.WeatherStale);
        Assert.Same(harness.Weather.Result.Weather, harness.Device.getUpdates()[0].Weather);
        Assert.Contains("connection refused", harness.Sink.LastFrame.WeatherInfo);
        Assert.Equal("salzburg", harness.Weather.LastLocationId);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_WeatherNull_PassesNullAndSetsFlag()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(true);
        WeatherResult noWeather = new WeatherResult();
        noWeather.ErrorMessage = "weather service down";
        harness.Weather.Result = noWeather;

        // Act
        await harness.startAsync();
        await harness.tickAsync();

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(2, updates.Count);
        Assert.Null(updates[1].Weather);
        Assert.True(harness.Sink.LastFrame.WeatherStale);
        Assert.Contains("weather service down", harness.Sink.LastFrame.WeatherInfo);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_WeatherFresh_NoFlag()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(true);

        // Act
        await harness.startAsync();
        await harness.tickAsync();

        // Assert
        Assert.False(harness.Sink.LastFrame.WeatherStale);
        Assert.Equal(2, harness.Weather.CallCount);
        await harness.stopAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_DeviceWithoutWeather_NeverCallsWeatherClient(bool useWeatherClient)
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        harness.UseWeatherClient = useWeatherClient;

        // Act
        await harness.startAsync();
        await harness.tickAsync();

        // Assert
        Assert.Equal(0, harness.Weather.CallCount);
        Assert.Null(harness.Device.getUpdates()[1].Weather);
        Assert.False(harness.Sink.LastFrame.WeatherStale);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_RejectedCommand_ShowsMessageAndKeepsSequence()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        uint sequenceBefore = harness.Sink.LastFrame.Sequence;
        harness.Clock.Status = ClockStatus.Paused;

        // Act
        harness.Commands.Writer.TryWrite(new DeviceCommand(FakeDevice.REJECTED_COMMAND, 999));
        await harness.tickAsync();
        SimulationFrame flagged = harness.Sink.LastFrame;

        // Assert
        Assert.Equal("value out of range", flagged.LastCommandMessage);
        Assert.Equal(sequenceBefore, flagged.Sequence); // rejected => state unchanged
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_AppliedCommand_BumpsSequence()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        harness.Clock.Status = ClockStatus.Paused;
        await harness.tickAsync();
        uint sequenceBefore = harness.Sink.LastFrame.Sequence;

        // Act
        harness.Commands.Writer.TryWrite(new DeviceCommand(CommonCommands.TURN_OFF, 0));
        await harness.tickAsync();
        uint sequenceAfterCommand = harness.Sink.LastFrame.Sequence;
        await harness.tickAsync();

        // Assert
        Assert.Equal(sequenceBefore + 1, sequenceAfterCommand);
        Assert.Equal(sequenceAfterCommand, harness.Sink.LastFrame.Sequence);
        Assert.Equal(CommonCommands.TURN_OFF, harness.Device.getCommands()[0].Name);
        Assert.Equal("ok", harness.Sink.LastFrame.LastCommandMessage);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_SinkThrows_LoopContinues()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        harness.Sink.ThrowOnPublish = true;

        // Act
        await harness.startAsync();
        await harness.tickAsync();
        harness.Sink.ThrowOnPublish = false;
        await harness.tickAsync();

        // Assert
        Assert.Equal(3, harness.Sink.FrameCount);
        Assert.False(harness.RunTask.IsCompleted);
        Assert.Equal(3, harness.Device.getUpdates().Count);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_DeviceThrows_ErrorVisibleAndLoopContinues()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        harness.Device.ThrowOnUpdate = true;

        // Act
        await harness.tickAsync();
        SimulationFrame failed = harness.Sink.LastFrame;
        await harness.tickAsync();
        harness.Device.ThrowOnUpdate = false;
        await harness.tickAsync();

        // Assert
        Assert.Contains("model broken", failed.DeviceError);
        Assert.False(harness.RunTask.IsCompleted);
        Assert.Equal(string.Empty, harness.Sink.LastFrame.DeviceError);
        Assert.Equal(2, harness.Device.getUpdates().Count); // initial + recovered tick
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_Cancellation_StopsCleanly()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        await harness.tickAsync();

        // Act
        await harness.stopAsync();
        int framesAfterStop = harness.Sink.FrameCount;
        harness.TimeProvider.Advance(harness.Tick * 5);

        // Assert
        Assert.True(harness.RunTask.IsCompletedSuccessfully);
        Assert.Equal(framesAfterStop, harness.Sink.FrameCount);
    }

    [Fact]
    public async Task RunAsync_StepClamp_LimitsAbsurdStep()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        harness.Options.MaxStepSeconds = 60;
        await harness.startAsync();

        // Act
        harness.Clock.VirtualNow = harness.Clock.VirtualNow.AddHours(5);
        await harness.advanceRealTimeAsync(harness.Tick);

        // Assert
        List<DeviceUpdateCall> updates = harness.Device.getUpdates();
        Assert.Equal(TimeSpan.FromSeconds(60), updates[updates.Count - 1].Step);
        await harness.stopAsync();
    }

    [Fact]
    public async Task RunAsync_BackwardReadingSameGeneration_ZeroStepNoUpdate()
    {
        // Arrange
        LoopHarness harness = new LoopHarness(false);
        await harness.startAsync();
        int updatesBefore = harness.Device.getUpdates().Count;

        // Act
        harness.Clock.VirtualNow = harness.Clock.VirtualNow.AddSeconds(-3);
        await harness.advanceRealTimeAsync(harness.Tick);

        // Assert
        Assert.Equal(updatesBefore, harness.Device.getUpdates().Count);
        Assert.Single(harness.Device.getResets());
        await harness.stopAsync();
    }

    [Fact]
    public void Constructor_DeviceNeedsWeatherWithoutClient_Throws()
    {
        // Arrange
        FakeDevice device = new FakeDevice(true);
        SimulationOptions options = new SimulationOptions();
        options.LocationId = "salzburg";

        // Act
        Exception exception = Record.Exception(delegate
        {
            new DeviceSimulationLoop(
                device,
                new FakeClockSource(),
                null,
                new FakeSimulationSink(),
                System.Threading.Channels.Channel.CreateUnbounded<DeviceCommand>().Reader,
                options,
                TimeProvider.System,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DeviceSimulationLoop>.Instance);
        });

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }
}
