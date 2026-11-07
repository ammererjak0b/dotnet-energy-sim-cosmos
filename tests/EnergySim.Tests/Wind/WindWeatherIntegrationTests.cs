using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.Weather;
using EnergySim.Wind;

namespace EnergySim.Tests.Wind;

/// <summary>Checks wind generation and weather warnings through the shared simulation loop.</summary>
public class WindWeatherIntegrationTests
{
    /// <summary>Uses the configured weather location and integrates the supplied virtual duration.</summary>
    [Fact]
    public async Task RunAsync_FreshWeather_GeneratesFromSharedClient()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        harness.Options.LocationId = "wien";
        WeatherDto weather = new WeatherDto();
        weather.LocationId = "wien";
        weather.WindSpeedMs = 12.0;
        harness.Weather.Result.Weather = weather;
        SimulationFrame initialFrame = await harness.startAsync();

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal("wien", harness.Weather.LastLocationId);
        Assert.Equal(0L, initialFrame.Header.EnergyWh);
        Assert.Equal(100000, initialFrame.Header.ActivePowerW);
        Assert.Equal(100000, frame.Header.ActivePowerW);
        Assert.Equal(100000L, frame.Header.EnergyWh);
        Assert.Equal(DeviceState.Running, frame.Header.State);
        Assert.False(frame.WeatherStale);
        Assert.Equal(string.Empty, frame.DeviceError);
    }

    /// <summary>Keeps the device available without producing energy before any weather arrives.</summary>
    [Fact]
    public async Task RunAsync_NoWeather_KeepsGenerationAtZeroAndPublishesWarning()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        harness.Weather.Result.Weather = null;
        harness.Weather.Result.ErrorMessage = "weather service unreachable";
        await harness.startAsync();

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(0, frame.Header.ActivePowerW);
        Assert.Equal(0L, frame.Header.EnergyWh);
        Assert.Equal(DeviceState.Standby, frame.Header.State);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, harness.Device.OperatingStatus);
        Assert.True(frame.WeatherStale);
        Assert.Contains("weather service unreachable", frame.WeatherInfo);
        Assert.Equal((ushort)0, frame.Header.FaultCode);
        Assert.Equal(string.Empty, frame.DeviceError);
    }

    /// <summary>Starts generation when the first usable weather value becomes available.</summary>
    [Fact]
    public async Task RunAsync_FirstWeatherAfterOutage_StartsGenerationAndClearsWarning()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        harness.Weather.Result.Weather = null;
        harness.Weather.Result.ErrorMessage = "weather service unreachable";
        await harness.startAsync();
        SimulationFrame missingFrame = await harness.advanceAsync(TimeSpan.FromHours(1));
        harness.Weather.Result = createWeatherResult(12.0);

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(0L, missingFrame.Header.EnergyWh);
        Assert.True(missingFrame.WeatherStale);
        Assert.Equal(100000, frame.Header.ActivePowerW);
        Assert.Equal(25000L, frame.Header.EnergyWh);
        Assert.Equal(DeviceState.Running, frame.Header.State);
        Assert.False(frame.WeatherStale);
        Assert.DoesNotContain("unreachable", frame.WeatherInfo);
    }

    /// <summary>Uses the last value supplied by the client while surfacing its stale flag separately.</summary>
    [Fact]
    public async Task RunAsync_StaleWeather_ContinuesGenerationAndPublishesWarning()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        WeatherResult freshResult = createWeatherResult(12.0);
        harness.Weather.Result = freshResult;
        await harness.startAsync();
        await harness.advanceAsync(TimeSpan.FromMinutes(15));
        WeatherResult staleResult = new WeatherResult();
        staleResult.Weather = freshResult.Weather;
        staleResult.IsStale = true;
        staleResult.ErrorMessage = "weather service timeout";
        harness.Weather.Result = staleResult;

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(100000, frame.Header.ActivePowerW);
        Assert.Equal(50000L, frame.Header.EnergyWh);
        Assert.Equal(DeviceState.Running, frame.Header.State);
        Assert.Equal(WindPlantStatus.Running, harness.Device.OperatingStatus);
        Assert.True(frame.WeatherStale);
        Assert.Contains("stale", frame.WeatherInfo);
        Assert.Contains("weather service timeout", frame.WeatherInfo);
        Assert.Equal((ushort)0, frame.Header.FaultCode);
        Assert.Equal(string.Empty, frame.DeviceError);
    }

    /// <summary>Replaces stale wind with fresh wind and removes the communication warning.</summary>
    [Fact]
    public async Task RunAsync_FreshWeatherAfterStale_UsesNewWindAndClearsWarning()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        harness.Weather.Result = createWeatherResult(12.0);
        harness.Weather.Result.IsStale = true;
        harness.Weather.Result.ErrorMessage = "weather service timeout";
        await harness.startAsync();
        SimulationFrame staleFrame = await harness.advanceAsync(TimeSpan.FromMinutes(15));
        harness.Weather.Result = createWeatherResult(0.0);

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromMinutes(15));

        // Assert
        Assert.True(staleFrame.WeatherStale);
        Assert.Equal(25000L, staleFrame.Header.EnergyWh);
        Assert.Equal(0.0, harness.Device.WindSpeedMs);
        Assert.Equal(0, frame.Header.ActivePowerW);
        Assert.Equal(25000L, frame.Header.EnergyWh);
        Assert.Equal(WindPlantStatus.Standby, harness.Device.OperatingStatus);
        Assert.False(frame.WeatherStale);
        Assert.DoesNotContain("stale", frame.WeatherInfo);
        Assert.DoesNotContain("timeout", frame.WeatherInfo);
    }

    /// <summary>Preserves accumulated energy if the weather contract supplies no value later.</summary>
    [Fact]
    public async Task RunAsync_WeatherBecomesUnavailable_StopsGenerationAndPreservesEnergy()
    {
        // Arrange
        await using WindWeatherLoopHarness harness = new WindWeatherLoopHarness();
        harness.Weather.Result = createWeatherResult(12.0);
        await harness.startAsync();
        await harness.advanceAsync(TimeSpan.FromMinutes(15));
        WeatherResult missingResult = new WeatherResult();
        missingResult.ErrorMessage = "no usable weather";
        harness.Weather.Result = missingResult;

        // Act
        SimulationFrame frame = await harness.advanceAsync(TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(0, frame.Header.ActivePowerW);
        Assert.Equal(25000L, frame.Header.EnergyWh);
        Assert.Equal(WindPlantStatus.WeatherUnavailable, harness.Device.OperatingStatus);
        Assert.True(frame.WeatherStale);
        Assert.Contains("no usable weather", frame.WeatherInfo);
        Assert.Equal((ushort)0, frame.Header.FaultCode);
    }

    /// <summary>Creates deterministic weather in the same result type used by local and HTTP clients.</summary>
    private static WeatherResult createWeatherResult(double windSpeedMs)
    {
        WeatherDto weather = new WeatherDto();
        weather.LocationId = "salzburg";
        weather.WindSpeedMs = windSpeedMs;
        WeatherResult result = new WeatherResult();
        result.Weather = weather;
        return result;
    }
}
