using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

public class SunPositionTests
{
    private const double SALZBURG_LATITUDE = 47.80;
    private const double SALZBURG_LONGITUDE = 13.04;
    private const double TOLERANCE_DEG = 0.5;

    // Solar noon at 13.04 E is about 11:09 UTC (summer) and 11:06 UTC (winter), 4 min per deg longitude plus equation of time
    [Fact]
    public void Elevation_SummerSolsticeNoon_MatchesReference()
    {
        // Arrange
        DateTimeOffset noon = new DateTimeOffset(2025, 6, 21, 11, 9, 0, TimeSpan.Zero);

        // Act
        double elevation = SunPosition.elevationDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, noon);

        // Assert
        Assert.InRange(elevation, 65.6 - TOLERANCE_DEG, 65.6 + TOLERANCE_DEG);
    }

    [Fact]
    public void Elevation_WinterSolsticeNoon_MatchesReference()
    {
        // Arrange
        DateTimeOffset noon = new DateTimeOffset(2025, 12, 21, 11, 6, 0, TimeSpan.Zero);

        // Act
        double elevation = SunPosition.elevationDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, noon);

        // Assert
        Assert.InRange(elevation, 18.7 - TOLERANCE_DEG, 18.7 + TOLERANCE_DEG);
    }

    [Fact]
    public void Elevation_Midnight_IsNegative()
    {
        // Arrange
        DateTimeOffset midnight = new DateTimeOffset(2025, 6, 21, 23, 0, 0, TimeSpan.Zero);

        // Act
        double elevation = SunPosition.elevationDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, midnight);

        // Assert
        Assert.True(elevation < 0);
    }

    [Fact]
    public void Azimuth_SolarNoon_IsSouth()
    {
        // Arrange
        DateTimeOffset noon = new DateTimeOffset(2025, 6, 21, 11, 9, 0, TimeSpan.Zero);

        // Act
        double azimuth = SunPosition.azimuthDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, noon);

        // Assert
        Assert.InRange(azimuth, 180 - 3, 180 + 3); // clockwise from north
    }

    [Fact]
    public void Azimuth_Morning_IsEastAfternoonIsWest()
    {
        // Arrange
        DateTimeOffset morning = new DateTimeOffset(2025, 6, 21, 6, 0, 0, TimeSpan.Zero);
        DateTimeOffset evening = new DateTimeOffset(2025, 6, 21, 16, 0, 0, TimeSpan.Zero);

        // Act
        double morningAzimuth = SunPosition.azimuthDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, morning);
        double eveningAzimuth = SunPosition.azimuthDeg(SALZBURG_LATITUDE, SALZBURG_LONGITUDE, evening);

        // Assert
        Assert.InRange(morningAzimuth, 60, 120);
        Assert.InRange(eveningAzimuth, 240, 300);
    }
}
