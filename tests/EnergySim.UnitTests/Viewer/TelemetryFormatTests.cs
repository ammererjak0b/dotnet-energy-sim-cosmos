using EnergySim.Messaging;
using EnergySim.Viewer;

namespace EnergySim.UnitTests.Viewer;

/// <summary>Console text of one telemetry message: virtual time, quality, marks for Bad and Uncertain, values with units.</summary>
public sealed class TelemetryFormatTests
{
    private static Measurement createMeasurement(string name, double value, string unit)
    {
        Measurement measurement = new Measurement();
        measurement.Name = name;
        measurement.Value = value;
        measurement.Unit = unit;
        return measurement;
    }

    private static TelemetryMessage createGoodMessage()
    {
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = "pv-1";
        message.SimTimeUtc = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        message.Sequence = 7;
        message.State = "Running";
        message.ClockState = "Running";
        message.Quality = TelemetryQuality.Good;
        message.Measurements.Add(createMeasurement("ActivePower", 3008, "W"));
        message.Measurements.Add(createMeasurement("ModuleTemperature", 40.25, "degC"));
        return message;
    }

    [Fact]
    public void Format_GoodMessage_ShowsHeaderLineAndValuesWithUnit()
    {
        // Act
        string text = TelemetryFormat.format(createGoodMessage());

        // Assert
        string[] lines = text.Split(Environment.NewLine);
        Assert.Equal("[pv-1] sim 2026-06-21 12:00:00Z  seq 7  Good  Running", lines[0]);
        Assert.Equal("    ActivePower = 3008 W", lines[1]);
        Assert.Equal("    ModuleTemperature = 40.25 degC", lines[2]);
    }

    [Fact]
    public void Format_BadMessage_MarksBadWithReasonAndShowsNoSimTime()
    {
        // Arrange
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = "wind-1";
        message.Quality = TelemetryQuality.Bad;
        message.Error = "cannot reach 127.0.0.1:1514";

        // Act
        string text = TelemetryFormat.format(message);

        // Assert
        Assert.Contains("sim -", text);
        Assert.Contains("!! BAD: cannot reach 127.0.0.1:1514", text);
    }

    [Fact]
    public void Format_UncertainMessage_MarksUncertainWithWarningsAndClock()
    {
        // Arrange
        TelemetryMessage message = createGoodMessage();
        message.Quality = TelemetryQuality.Uncertain;
        message.WarnFlags.Add("NoMasterSync");

        // Act
        string text = TelemetryFormat.format(message);

        // Assert
        Assert.Contains("! UNCERTAIN: NoMasterSync clock Running", text);
    }
}
