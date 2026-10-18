using EnergySim.Pv;

namespace EnergySim.Tests.Pv;

public class PvOptionsTests
{
    [Fact]
    public void Defaults_AreFiveKwpAndPerformanceRatioEightTenths()
    {
        // Arrange
        PvOptions options = new PvOptions();

        // Act
        options.validate();

        // Assert
        Assert.Equal(5.0, options.PeakPowerKw);
        Assert.Equal(0.8, options.PerformanceRatio);
        Assert.Equal("Pv", PvOptions.SECTION_NAME);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void Validate_WithInvalidPeakPower_Throws(double peakPowerKw)
    {
        // Arrange
        PvOptions options = new PvOptions();
        options.PeakPowerKw = peakPowerKw;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());

        // Assert
        Assert.Contains("PeakPowerKw", exception.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Validate_WithInvalidPerformanceRatio_Throws(double performanceRatio)
    {
        // Arrange
        PvOptions options = new PvOptions();
        options.PerformanceRatio = performanceRatio;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());

        // Assert
        Assert.Contains("PerformanceRatio", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyDeviceId_Throws()
    {
        // Arrange, Act, Assert
        Assert.Throws<ArgumentException>(() => new PvDevice(" ", new PvOptions()));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_Throws()
    {
        // Arrange
        PvOptions options = new PvOptions();
        options.PeakPowerKw = 0.0;

        // Act, Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new PvDevice("pv-1", options));
    }
}
