using EnergySim.Wind;

namespace EnergySim.Tests.Wind;

/// <summary>Checks the wind plant settings independently of weather and simulation time.</summary>
public class WindOptionsTests
{
    /// <summary>Uses the agreed demonstration values for an unconfigured wind plant.</summary>
    [Fact]
    public void Constructor_WhenCreated_UsesDemonstrationSettings()
    {
        // Arrange
        const double EXPECTED_RATED_POWER_KW = 100.0;
        const double EXPECTED_CUT_IN_WIND_SPEED_MS = 3.0;
        const double EXPECTED_RATED_WIND_SPEED_MS = 12.0;
        const double EXPECTED_CUT_OUT_WIND_SPEED_MS = 25.0;
        const double EXPECTED_RESTART_WIND_SPEED_MS = 20.0;

        // Act
        WindOptions options = new WindOptions();

        // Assert
        Assert.Equal(EXPECTED_RATED_POWER_KW, options.RatedPowerKw);
        Assert.Equal(EXPECTED_CUT_IN_WIND_SPEED_MS, options.CutInWindSpeedMs);
        Assert.Equal(EXPECTED_RATED_WIND_SPEED_MS, options.RatedWindSpeedMs);
        Assert.Equal(EXPECTED_CUT_OUT_WIND_SPEED_MS, options.CutOutWindSpeedMs);
        Assert.Equal(EXPECTED_RESTART_WIND_SPEED_MS, options.RestartWindSpeedMs);
    }

    /// <summary>Rejects nonpositive or nonfinite electrical rated power.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidRatedPower_Throws(double ratedPowerKw)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = ratedPowerKw;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.validate);

        // Assert
        Assert.Equal(nameof(WindOptions.RatedPowerKw), exception.ParamName);
    }

    /// <summary>Rejects negative or nonfinite cut-in wind speed.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidCutInWindSpeed_Throws(double cutInWindSpeedMs)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.CutInWindSpeedMs = cutInWindSpeedMs;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.validate);

        // Assert
        Assert.Equal(nameof(WindOptions.CutInWindSpeedMs), exception.ParamName);
    }

    /// <summary>Requires finite rated wind speed strictly above cut-in speed.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidRatedWindSpeed_Throws(double ratedWindSpeedMs)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedWindSpeedMs = ratedWindSpeedMs;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.validate);

        // Assert
        Assert.Equal(nameof(WindOptions.RatedWindSpeedMs), exception.ParamName);
    }

    /// <summary>Requires finite cut-out wind speed strictly above rated wind speed.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(11.0)]
    [InlineData(12.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidCutOutWindSpeed_Throws(double cutOutWindSpeedMs)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.CutOutWindSpeedMs = cutOutWindSpeedMs;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.validate);

        // Assert
        Assert.Equal(nameof(WindOptions.CutOutWindSpeedMs), exception.ParamName);
    }

    /// <summary>Requires a finite restart threshold between cut-in and cut-out wind speed.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    [InlineData(25.0)]
    [InlineData(26.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Validate_WithInvalidRestartWindSpeed_Throws(double restartWindSpeedMs)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RestartWindSpeedMs = restartWindSpeedMs;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(options.validate);

        // Assert
        Assert.Equal(nameof(WindOptions.RestartWindSpeedMs), exception.ParamName);
    }

    /// <summary>Accepts ordered thresholds including zero cut-in and restart below rated speed.</summary>
    [Theory]
    [InlineData(100.0, 3.0, 12.0, 25.0, 20.0)]
    [InlineData(1.0, 0.0, 1.0, 2.0, 0.5)]
    [InlineData(250.0, 4.0, 10.0, 22.0, 10.0)]
    [InlineData(500.0, 5.0, 15.0, 30.0, 5.1)]
    [InlineData(100.0, 3.0, 12.0, 25.0, 24.9)]
    public void Validate_WithValidSettings_DoesNotThrow(
        double ratedPowerKw,
        double cutInWindSpeedMs,
        double ratedWindSpeedMs,
        double cutOutWindSpeedMs,
        double restartWindSpeedMs)
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = ratedPowerKw;
        options.CutInWindSpeedMs = cutInWindSpeedMs;
        options.RatedWindSpeedMs = ratedWindSpeedMs;
        options.CutOutWindSpeedMs = cutOutWindSpeedMs;
        options.RestartWindSpeedMs = restartWindSpeedMs;

        // Act
        Exception? exception = Record.Exception(options.validate);

        // Assert
        Assert.Null(exception);
    }

    /// <summary>Uses the same section naming scheme as the existing device modules.</summary>
    [Fact]
    public void SectionName_WhenRequested_ReturnsWind()
    {
        // Arrange
        const string EXPECTED_SECTION_NAME = "Wind";

        // Act
        string sectionName = WindOptions.SECTION_NAME;

        // Assert
        Assert.Equal(EXPECTED_SECTION_NAME, sectionName);
    }
}
