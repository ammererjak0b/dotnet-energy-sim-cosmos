using EnergySim.Core.Weather;
using EnergySim.Wind;

namespace EnergySim.Tests.Wind;

/// <summary>Checks wind power and energy using fixed weather values and virtual time steps.</summary>
public class WindPlantTests
{
    private const int DECIMAL_PRECISION = 9;

    private static readonly TimeSpan ONE_HOUR = TimeSpan.FromHours(1);

    /// <summary>Starts on without weather, generation or accumulated energy.</summary>
    [Fact]
    public void Constructor_WithValidOptions_StartsWaitingForWeather()
    {
        // Arrange
        WindOptions options = new WindOptions();

        // Act
        WindPlant plant = new WindPlant(options);

        // Assert
        Assert.True(plant.IsOn);
        Assert.False(plant.HasWeather);
        Assert.Equal(100.0, plant.PowerLimitPercent);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, plant.Status);
    }

    /// <summary>Rejects missing configuration before creating a plant.</summary>
    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        // Arrange
        WindOptions options = null!;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(delegate
        {
            new WindPlant(options);
        });

        // Assert
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>Validates configuration at the model boundary.</summary>
    [Fact]
    public void Constructor_WithInvalidOptions_Throws()
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = 0.0;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(delegate
        {
            new WindPlant(options);
        });

        // Assert
        Assert.Equal(nameof(WindOptions.RatedPowerKw), exception.ParamName);
    }

    /// <summary>Accepts calm weather without generating power up to the cut-in threshold.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(2.9)]
    [InlineData(3.0)]
    public void Update_WithWindAtOrBelowCutIn_StaysOnStandby(double windSpeedMs)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.True(plant.HasWeather);
        Assert.Equal(windSpeedMs, plant.WindSpeedMs);
        Assert.Equal(0.0, plant.AvailablePowerKw);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(WindPlantStatus.Standby, plant.Status);
    }

    /// <summary>Caps production at rated power in the rated operating range.</summary>
    [Theory]
    [InlineData(12.0)]
    [InlineData(20.0)]
    [InlineData(24.9)]
    public void Update_WithWindAtOrAboveRated_ProducesRatedPower(double windSpeedMs)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(TimeSpan.Zero, weather);

        // Assert
        Assert.Equal(100.0, plant.AvailablePowerKw);
        Assert.Equal(100.0, plant.PowerKw);
        Assert.Equal(WindPlantStatus.Running, plant.Status);
    }

    /// <summary>Interpolates between cut-in and rated speed with the agreed cubic curve.</summary>
    [Theory]
    [InlineData(6.0, 100.0 / 9.0)]
    [InlineData(9.0, 2600.0 / 63.0)]
    public void Update_WithWindBetweenCutInAndRated_UsesCubicPowerCurve(double windSpeedMs, double expectedPowerKw)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(TimeSpan.Zero, weather);

        // Assert
        Assert.Equal(expectedPowerKw, plant.PowerKw, DECIMAL_PRECISION);
        Assert.Equal(expectedPowerKw, plant.AvailablePowerKw, DECIMAL_PRECISION);
        Assert.Equal(WindPlantStatus.Running, plant.Status);
    }

    /// <summary>Integrates electrical power over virtual hours into watt-hours.</summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(15.0, 25000.0)]
    [InlineData(60.0, 100000.0)]
    public void Update_WithRatedWind_AddsEnergyForVirtualDuration(double stepMinutes, double expectedEnergyWh)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);

        // Act
        plant.update(TimeSpan.FromMinutes(stepMinutes), weather);

        // Assert
        Assert.Equal(expectedEnergyWh, plant.TotalEnergyWh, DECIMAL_PRECISION);
    }

    /// <summary>Rejects negative time before changing the last accepted state.</summary>
    [Fact]
    public void Update_WithNegativeStep_ThrowsAndKeepsState()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);
        WeatherDto calmWeather = createWeather(0.0);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(delegate
        {
            plant.update(TimeSpan.FromMinutes(-1.0), calmWeather);
        });

        // Assert
        Assert.Equal("step", exception.ParamName);
        Assert.Equal(100.0, plant.PowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
    }

    /// <summary>Stops generation at and above the storm cut-out threshold.</summary>
    [Theory]
    [InlineData(25.0)]
    [InlineData(40.0)]
    public void Update_WithWindAtOrAboveCutOut_EntersStormShutdown(double windSpeedMs)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.True(plant.IsStormShutdown);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.AvailablePowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.StormShutdown, plant.Status);
    }

    /// <summary>Keeps storm protection active until wind is strictly below restart speed.</summary>
    [Theory]
    [InlineData(24.0, true, 0.0)]
    [InlineData(20.0, true, 0.0)]
    [InlineData(19.9, false, 100.0)]
    public void Update_AfterStorm_RespectsRestartThreshold(double windSpeedMs, bool expectedShutdown, double expectedPowerKw)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto storm = createWeather(25.0);
        plant.update(TimeSpan.Zero, storm);
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.Equal(expectedShutdown, plant.IsStormShutdown);
        Assert.Equal(expectedPowerKw, plant.PowerKw);
        Assert.Equal(expectedPowerKw * 1000.0, plant.TotalEnergyWh);
    }

    /// <summary>Does not continue generating from an old wind value when weather is missing.</summary>
    [Fact]
    public void Update_WithMissingWeather_StopsPowerAndKeepsEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);

        // Act
        plant.update(ONE_HOUR, null);

        // Assert
        Assert.False(plant.HasWeather);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.AvailablePowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, plant.Status);
    }

    /// <summary>Rejects nonphysical wind values without adding energy.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Update_WithInvalidWind_MarksWeatherUnavailable(double windSpeedMs)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);

        // Act
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.False(plant.HasWeather);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, plant.Status);
    }

    /// <summary>Stops immediately and remains off during subsequent virtual time steps.</summary>
    [Fact]
    public void TurnOff_WhileGenerating_StopsPowerAndEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);

        // Act
        plant.turnOff();
        double powerAfterTurnOffKw = plant.PowerKw;
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.False(plant.IsOn);
        Assert.Equal(0.0, powerAfterTurnOffKw);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Off, plant.Status);
    }

    /// <summary>Resumes from the last valid weather without adding energy through a command.</summary>
    [Fact]
    public void TurnOn_AfterTurnOff_ResumesPowerWithoutAddingEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);
        plant.turnOff();

        // Act
        plant.turnOn();

        // Assert
        Assert.True(plant.IsOn);
        Assert.Equal(100.0, plant.PowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Running, plant.Status);
    }

    /// <summary>Limits actual power immediately while keeping available wind power visible.</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(50.0)]
    public void SetPowerLimit_BelowAvailablePower_LimitsGeneration(double limitPercent)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(TimeSpan.Zero, weather);

        // Act
        plant.setPowerLimit(limitPercent);
        double powerAfterCommandKw = plant.PowerKw;
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.Equal(limitPercent, plant.PowerLimitPercent);
        Assert.Equal(limitPercent, powerAfterCommandKw);
        Assert.Equal(limitPercent, plant.PowerKw);
        Assert.Equal(100.0, plant.AvailablePowerKw);
        Assert.Equal(limitPercent * 1000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Limited, plant.Status);
    }

    /// <summary>Rejects invalid limits before altering power or settings.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(101.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void SetPowerLimit_WithInvalidPercent_ThrowsAndKeepsLimit(double limitPercent)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(delegate
        {
            plant.setPowerLimit(limitPercent);
        });

        // Assert
        Assert.Equal("percent", exception.ParamName);
        Assert.Equal(100.0, plant.PowerLimitPercent);
        Assert.Equal(0.0, plant.PowerKw);
    }

    /// <summary>Keeps a manually triggered technical fault separate from storm protection.</summary>
    [Fact]
    public void TriggerFault_WhileGenerating_StopsPowerAndEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);

        // Act
        plant.triggerFault();
        double powerAfterFaultKw = plant.PowerKw;
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.True(plant.HasFault);
        Assert.False(plant.IsStormShutdown);
        Assert.Equal(0.0, powerAfterFaultKw);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Fault, plant.Status);
    }

    /// <summary>Clears only the technical fault and keeps storm protection effective.</summary>
    [Theory]
    [InlineData(12.0, 100.0, WindPlantStatus.Running)]
    [InlineData(25.0, 0.0, WindPlantStatus.StormShutdown)]
    public void ResetFault_WithKnownWeather_PreservesStormProtection(double windSpeedMs, double expectedPowerKw, WindPlantStatus expectedStatus)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);
        plant.update(TimeSpan.Zero, weather);
        plant.triggerFault();

        // Act
        plant.resetFault();

        // Assert
        Assert.False(plant.HasFault);
        Assert.Equal(expectedPowerKw, plant.PowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(expectedStatus, plant.Status);
    }

    /// <summary>Restarts a run with initial controls, no weather and an empty energy counter.</summary>
    [Fact]
    public void Reset_AfterOperation_RestoresInitialState()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);
        plant.setPowerLimit(50.0);
        WeatherDto storm = createWeather(25.0);
        plant.update(TimeSpan.Zero, storm);
        plant.triggerFault();
        plant.turnOff();

        // Act
        plant.reset();

        // Assert
        Assert.True(plant.IsOn);
        Assert.False(plant.HasFault);
        Assert.False(plant.HasWeather);
        Assert.False(plant.IsStormShutdown);
        Assert.Equal(100.0, plant.PowerLimitPercent);
        Assert.Equal(0.0, plant.WindSpeedMs);
        Assert.Equal(0.0, plant.AvailablePowerKw);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, plant.Status);
    }

    /// <summary>Uses the validated configuration even if the caller later edits its options object.</summary>
    [Fact]
    public void Constructor_AfterOptionsChange_KeepsValidatedConfiguration()
    {
        // Arrange
        WindOptions options = new WindOptions();
        WindPlant plant = new WindPlant(options);
        options.RatedPowerKw = 1000.0;
        options.CutInWindSpeedMs = 0.0;
        options.RatedWindSpeedMs = 60.0;
        options.CutOutWindSpeedMs = 250.0;
        options.RestartWindSpeedMs = 200.0;
        WeatherDto weather = createWeather(12.0);

        // Act
        plant.update(TimeSpan.Zero, weather);

        // Assert
        Assert.Equal(100.0, plant.PowerKw);
        Assert.Equal(WindPlantStatus.Running, plant.Status);
    }

    /// <summary>Uses configured power and thresholds instead of demonstration constants.</summary>
    [Fact]
    public void Update_WithCustomOptions_UsesConfiguredCurve()
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = 250.0;
        options.CutInWindSpeedMs = 0.0;
        options.RatedWindSpeedMs = 10.0;
        options.CutOutWindSpeedMs = 20.0;
        options.RestartWindSpeedMs = 15.0;
        WindPlant plant = new WindPlant(options);
        WeatherDto weather = createWeather(5.0);

        // Act
        plant.update(ONE_HOUR, weather);

        // Assert
        Assert.Equal(31.25, plant.PowerKw, DECIMAL_PRECISION);
        Assert.Equal(31250.0, plant.TotalEnergyWh, DECIMAL_PRECISION);
    }

    /// <summary>Accumulates independent steps without rounding each energy increment.</summary>
    [Fact]
    public void Update_WithSeveralSteps_AccumulatesEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        TimeSpan step = TimeSpan.FromMinutes(20.0);

        // Act
        plant.update(step, weather);
        plant.update(step, weather);
        plant.update(step, weather);

        // Assert
        Assert.Equal(100000.0, plant.TotalEnergyWh, DECIMAL_PRECISION);
    }

    /// <summary>Updates displayed power on a zero step without adding energy.</summary>
    [Fact]
    public void Update_WithZeroStep_RefreshesPowerWithoutAddingEnergy()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);
        WeatherDto weakerWind = createWeather(6.0);

        // Act
        plant.update(TimeSpan.Zero, weakerWind);

        // Assert
        Assert.Equal(100.0 / 9.0, plant.PowerKw, DECIMAL_PRECISION);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
    }

    /// <summary>Clears earlier power when wind becomes calm.</summary>
    [Fact]
    public void Update_AfterWindDropsToCalm_StopsGeneration()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(12.0);
        plant.update(ONE_HOUR, weather);
        WeatherDto calmWeather = createWeather(0.0);

        // Act
        plant.update(ONE_HOUR, calmWeather);

        // Assert
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(100000.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Standby, plant.Status);
    }

    /// <summary>Does not clear a storm latch when weather is missing or invalid.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_AfterWeatherLossDuringStorm_PreservesShutdown(bool useInvalidWeather)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto storm = createWeather(25.0);
        plant.update(TimeSpan.Zero, storm);
        WeatherDto? unavailableWeather = null;
        if (useInvalidWeather)
        {
            unavailableWeather = new WeatherDto();
            unavailableWeather.WindSpeedMs = double.NaN;
        }

        plant.update(ONE_HOUR, unavailableWeather);
        WeatherDto recoveredWeather = createWeather(20.0);

        // Act
        plant.update(ONE_HOUR, recoveredWeather);

        // Assert
        Assert.True(plant.IsStormShutdown);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.StormShutdown, plant.Status);
    }

    /// <summary>Does not bypass storm protection through an off/on command sequence.</summary>
    [Fact]
    public void TurnOn_DuringStorm_KeepsGenerationStopped()
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto storm = createWeather(25.0);
        plant.update(TimeSpan.Zero, storm);
        plant.turnOff();

        // Act
        plant.turnOn();

        // Assert
        Assert.True(plant.IsStormShutdown);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(WindPlantStatus.StormShutdown, plant.Status);
    }

    /// <summary>A raised ceiling does not create power beyond what the wind provides.</summary>
    [Theory]
    [InlineData(6.0, 50.0, 100.0 / 9.0)]
    [InlineData(12.0, 100.0, 100.0)]
    public void SetPowerLimit_AboveAvailablePower_KeepsAvailableGeneration(double windSpeedMs, double limitPercent, double expectedPowerKw)
    {
        // Arrange
        WindPlant plant = new WindPlant(new WindOptions());
        WeatherDto weather = createWeather(windSpeedMs);
        plant.update(TimeSpan.Zero, weather);
        plant.setPowerLimit(0.0);

        // Act
        plant.setPowerLimit(limitPercent);

        // Assert
        Assert.Equal(expectedPowerKw, plant.PowerKw, DECIMAL_PRECISION);
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(WindPlantStatus.Running, plant.Status);
    }

    /// <summary>Creates fixed weather input without a clock or an external weather source.</summary>
    private static WeatherDto createWeather(double windSpeedMs)
    {
        WeatherDto weather = new WeatherDto();
        weather.WindSpeedMs = windSpeedMs;
        return weather;
    }
}
