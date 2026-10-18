using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

public class WeatherModelTests
{
    private static void assertSameWeather(WeatherDto expected, WeatherDto actual)
    {
        Assert.Equal(expected.TemperatureC, actual.TemperatureC);
        Assert.Equal(expected.CloudCoverPct, actual.CloudCoverPct);
        Assert.Equal(expected.GlobalIrradianceWm2, actual.GlobalIrradianceWm2);
        Assert.Equal(expected.PrecipitationMmPerH, actual.PrecipitationMmPerH);
        Assert.Equal(expected.WindSpeedMs, actual.WindSpeedMs);
        Assert.Equal(expected.WindDirectionDeg, actual.WindDirectionDeg);
    }

    [Fact]
    public void GetWeather_SameQuery_ReturnsSameValues()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());
        DateTimeOffset time = new DateTimeOffset(2026, 9, 23, 12, 17, 41, TimeSpan.Zero);

        // Act
        WeatherDto first = model.getWeather("salzburg", time);
        WeatherDto second = model.getWeather("salzburg", time);

        // Assert
        assertSameWeather(first, second);
        Assert.Equal(first.SunElevationDeg, second.SunElevationDeg);
        Assert.Equal(first.SunAzimuthDeg, second.SunAzimuthDeg);
    }

    [Fact]
    public void GetWeather_ShuffledQueryOrder_ReturnsSameValues()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());
        List<DateTimeOffset> times = new List<DateTimeOffset>();
        for (int step = 0; step < 50; step++)
        {
            times.Add(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(step * 7919));
        }
        Dictionary<DateTimeOffset, WeatherDto> inOrder = new Dictionary<DateTimeOffset, WeatherDto>();
        foreach (DateTimeOffset time in times)
        {
            inOrder[time] = model.getWeather("salzburg", time);
        }
        List<DateTimeOffset> shuffled = new List<DateTimeOffset>(times);
        Random random = new Random(42); // seeded
        for (int index = shuffled.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1); // Fisher-Yates
            DateTimeOffset temp = shuffled[index];
            shuffled[index] = shuffled[other];
            shuffled[other] = temp;
        }

        // Act
        Dictionary<DateTimeOffset, WeatherDto> shuffledResult = new Dictionary<DateTimeOffset, WeatherDto>();
        foreach (DateTimeOffset time in shuffled)
        {
            shuffledResult[time] = model.getWeather("salzburg", time);
        }

        // Assert
        foreach (DateTimeOffset time in times)
        {
            assertSameWeather(inOrder[time], shuffledResult[time]);
            Assert.Equal(inOrder[time].SunElevationDeg, shuffledResult[time].SunElevationDeg);
        }
    }

    [Fact]
    public void GetWeather_UnknownLocation_Throws()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());

        // Act
        Action act = () => model.getWeather("mars", DateTimeOffset.UnixEpoch);

        // Assert
        Assert.Throws<UnknownLocationException>(act);
    }

    [Fact]
    public void GetWeather_LeapDay_MapsToFeb28()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());

        // Act
        WeatherDto leapDay = model.getWeather("salzburg", new DateTimeOffset(2024, 2, 29, 12, 30, 0, TimeSpan.Zero));
        WeatherDto feb28 = model.getWeather("salzburg", new DateTimeOffset(2025, 2, 28, 12, 30, 0, TimeSpan.Zero));

        // Assert
        assertSameWeather(feb28, leapDay);
    }

    [Fact]
    public void GetWeather_YearIsIgnored()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());

        // Act
        WeatherDto year2025 = model.getWeather("salzburg", new DateTimeOffset(2025, 7, 14, 9, 20, 0, TimeSpan.Zero));
        WeatherDto year2031 = model.getWeather("salzburg", new DateTimeOffset(2031, 7, 14, 9, 20, 0, TimeSpan.Zero));

        // Assert
        assertSameWeather(year2025, year2031);
    }

    [Fact]
    public void GetWeather_BetweenHours_InterpolatesLinearly()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());
        int index = TestDataSets.hourIndex(3, 1, 12);

        // Act
        WeatherDto full = model.getWeather("salzburg", new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.Zero));
        WeatherDto quarter = model.getWeather("salzburg", new DateTimeOffset(2025, 3, 1, 12, 15, 0, TimeSpan.Zero));

        // Assert
        Assert.Equal(index, full.TemperatureC, 6);
        Assert.Equal(index + 0.25, quarter.TemperatureC, 6); // ramp +1 per hour
        Assert.Equal(full.PrecipitationMmPerH, quarter.PrecipitationMmPerH); // step value, no interpolation
    }

    [Fact]
    public void GetWeather_AfterLastHourOfYear_WrapsToFirstHour()
    {
        // Arrange
        WeatherDataSet dataSet = TestDataSets.createRamp();
        WeatherModel model = TestDataSets.createModel(dataSet);

        // Act
        WeatherDto result = model.getWeather("salzburg", new DateTimeOffset(2025, 12, 31, 23, 30, 0, TimeSpan.Zero));

        // Assert
        double expected = (dataSet.TemperatureC[8759] + dataSet.TemperatureC[0]) / 2.0;
        Assert.Equal(expected, result.TemperatureC, 6);
    }

    [Fact]
    public void GetWeather_WindDirectionAcrossNorth_WrapsAround()
    {
        // Arrange
        WeatherDataSet ramp = TestDataSets.createRamp();
        int index = TestDataSets.hourIndex(5, 10, 6);
        double[] direction = new double[WeatherDataSet.HOURS_PER_YEAR];
        direction[index] = 350;
        direction[index + 1] = 10;
        WeatherDataSet dataSet = new WeatherDataSet(ramp.TemperatureC, ramp.CloudCoverPct, ramp.ShortwaveRadiationWm2,
            ramp.PrecipitationMmPerH, ramp.WindSpeedMs, direction);
        WeatherModel model = TestDataSets.createModel(dataSet);

        // Act
        WeatherDto result = model.getWeather("salzburg", new DateTimeOffset(2025, 5, 10, 6, 30, 0, TimeSpan.Zero));

        // Assert
        Assert.InRange(result.WindDirectionDeg, 0, 360);
        double distanceToNorth = Math.Min(result.WindDirectionDeg, 360 - result.WindDirectionDeg);
        Assert.True(distanceToNorth < 0.001, "expected north, got " + result.WindDirectionDeg);
    }

    [Fact]
    public void GetWeather_Midsummer_FillsSunPosition()
    {
        // Arrange
        WeatherModel model = TestDataSets.createModel(TestDataSets.createRamp());

        // Act
        WeatherDto night = model.getWeather("salzburg", new DateTimeOffset(2025, 6, 21, 0, 0, 0, TimeSpan.Zero));
        WeatherDto noon = model.getWeather("salzburg", new DateTimeOffset(2025, 6, 21, 11, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.True(night.SunElevationDeg < 0);
        Assert.True(noon.SunElevationDeg > 60);
        Assert.Equal("salzburg", noon.LocationId);
    }
}
