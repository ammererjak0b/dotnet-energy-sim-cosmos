using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

public class VirtualTimeFormatTests
{
    [Fact]
    public void Format_InSummer_ShowsCestAndUtc()
    {
        // Arrange
        DateTimeOffset utc = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);

        // Act
        string text = VirtualTimeFormat.format(utc);

        // Assert
        Assert.Equal("2026-06-21 06:00 CEST (04:00Z)", text);
    }

    [Fact]
    public void Format_InWinter_ShowsCetAndUtc()
    {
        // Arrange
        DateTimeOffset utc = new DateTimeOffset(2026, 1, 15, 11, 30, 0, TimeSpan.Zero);

        // Act
        string text = VirtualTimeFormat.format(utc);

        // Assert
        Assert.Equal("2026-01-15 12:30 CET (11:30Z)", text);
    }

    [Fact]
    public void Format_WithUnknownZone_FallsBackToLabeledUtc()
    {
        // Arrange
        DateTimeOffset utc = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);

        // Act
        string text = VirtualTimeFormat.format(utc, "Nowhere/Missing");

        // Assert
        Assert.Equal("2026-06-21 04:00 UTC (no tz data) (04:00Z)", text);
    }
}
