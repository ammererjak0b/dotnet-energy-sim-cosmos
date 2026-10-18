using EnergySim.Battery;

namespace EnergySim.Tests.Battery;

public class BatteryAutoScheduleTests
{
    private static DateTimeOffset at(int hour, int minute, int second)
    {
        return new DateTimeOffset(2026, 7, 15, hour, minute, second, TimeSpan.Zero);
    }

    [Theory]
    [InlineData(3, -1, 0.20, 0.50)]
    [InlineData(8, 1, 0.20, 0.60)]
    [InlineData(12, -1, 0.10, 0.80)]
    [InlineData(18, 1, 0.30, 1.00)]
    [InlineData(23, -1, 0.10, 0.30)]
    public void GetRequestedPowerKW_FollowsSchedule(int hour, int sign, double minimumFactor, double maximumFactor)
    {
        // Arrange
        BatteryOptions options = new BatteryOptions();

        // Act
        double powerKw = BatteryAutoSchedule.getRequestedPowerKW(options, at(hour, 30, 0), 50.0);

        // Assert
        Assert.Equal(sign, Math.Sign(powerKw));
        Assert.InRange(Math.Abs(powerKw), 50.0 * minimumFactor, 50.0 * maximumFactor);
    }

    [Fact]
    public void GetRequestedPowerKW_SameMinute_IsIdenticalRegardlessOfSecondsAndCalls()
    {
        // Arrange
        BatteryOptions options = new BatteryOptions();

        // Act
        double first = BatteryAutoSchedule.getRequestedPowerKW(options, at(12, 7, 0), 50.0);
        double second = BatteryAutoSchedule.getRequestedPowerKW(options, at(12, 7, 41), 50.0);
        double third = BatteryAutoSchedule.getRequestedPowerKW(options, at(12, 7, 0), 50.0);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void GetRequestedPowerKW_DifferentMinutes_VaryWithinBand()
    {
        // Arrange
        BatteryOptions options = new BatteryOptions();
        HashSet<double> values = new HashSet<double>();

        // Act
        for (int minute = 0; minute < 30; minute++)
        {
            values.Add(BatteryAutoSchedule.getRequestedPowerKW(options, at(12, minute, 0), 50.0));
        }

        // Assert
        Assert.True(values.Count > 20);
    }

    [Fact]
    public void GetRequestedPowerKW_DifferentSeed_ChangesValue()
    {
        // Arrange
        BatteryOptions optionsA = new BatteryOptions();
        BatteryOptions optionsB = new BatteryOptions();
        optionsB.RandomSeed = 43;

        // Act
        double powerA = BatteryAutoSchedule.getRequestedPowerKW(optionsA, at(12, 7, 0), 50.0);
        double powerB = BatteryAutoSchedule.getRequestedPowerKW(optionsB, at(12, 7, 0), 50.0);

        // Assert
        Assert.NotEqual(powerA, powerB);
    }

    [Fact]
    public void GetRequestedPowerKW_FullBatteryWhileCharging_ReturnsZero()
    {
        BatteryOptions options = new BatteryOptions();

        double powerKw = BatteryAutoSchedule.getRequestedPowerKW(options, at(3, 0, 0), 90.0);

        Assert.Equal(0.0, powerKw);
    }

    [Fact]
    public void GetRequestedPowerKW_EmptyBatteryWhileDischarging_ReturnsZero()
    {
        BatteryOptions options = new BatteryOptions();

        double powerKw = BatteryAutoSchedule.getRequestedPowerKW(options, at(18, 0, 0), 10.0);

        Assert.Equal(0.0, powerKw);
    }

    [Fact]
    public void GetRequestedPowerKW_UsesUtcHour()
    {
        // Arrange
        BatteryOptions options = new BatteryOptions();
        DateTimeOffset local = new DateTimeOffset(2026, 7, 15, 5, 30, 0, TimeSpan.FromHours(-4)); // 09:30 UTC

        // Act
        double powerKw = BatteryAutoSchedule.getRequestedPowerKW(options, local, 50.0);

        // Assert
        Assert.True(powerKw > 0);
    }
}
