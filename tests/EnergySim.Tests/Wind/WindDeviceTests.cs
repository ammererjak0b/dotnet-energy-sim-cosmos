using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using EnergySim.Wind;

namespace EnergySim.Tests.Wind;

/// <summary>Checks the wind adapter through the shared device contract.</summary>
public class WindDeviceTests
{
    private static readonly DateTimeOffset VIRTUAL_NOW = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ONE_HOUR = TimeSpan.FromHours(1);

    /// <summary>Advertises a weather-dependent wind device with the common initial header.</summary>
    [Fact]
    public void Constructor_WithValidOptions_SetsWindIdentity()
    {
        // Arrange
        const string DEVICE_ID = "wind-7";

        // Act
        WindDevice device = new WindDevice(DEVICE_ID, new WindOptions());

        // Assert
        Assert.Equal(DEVICE_ID, device.DeviceId);
        Assert.Equal(DeviceType.Wind, device.Type);
        Assert.Equal(4, (int)device.Type);
        Assert.True(device.NeedsWeather);
        Assert.True(device.Header.IsOn);
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(0L, device.Header.EnergyWh);
        Assert.Equal((ushort)0, device.Header.FaultCode);
    }

    /// <summary>Publishes generation in watts and accumulated energy in watt-hours.</summary>
    [Fact]
    public void Update_WithRatedWind_FillsCommonHeaderInWattsAndWattHours()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());

        // Act
        device.update(VIRTUAL_NOW, TimeSpan.FromMinutes(15), createWeather(12.0));

        // Assert
        Assert.Equal(100000, device.Header.ActivePowerW);
        Assert.Equal(25000L, device.Header.EnergyWh);
        Assert.Equal(DeviceState.Running, device.Header.State);
    }

    /// <summary>Offers the same five controls as the other devices without custom keys.</summary>
    [Fact]
    public void Commands_WhenCreated_OffersCommonCommands()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        List<CommandDescriptor> expectedCommands = DeviceSupport.createCommonCommands();

        // Act
        IReadOnlyList<CommandDescriptor> commands = device.Commands;

        // Assert
        Assert.Equal(expectedCommands.Count, commands.Count);
        for (int index = 0; index < commands.Count; index++)
        {
            Assert.Equal(expectedCommands[index].Name, commands[index].Name);
            Assert.Equal(expectedCommands[index].Key, commands[index].Key);
            Assert.Equal(expectedCommands[index].HasParameter, commands[index].HasParameter);
        }
    }

    /// <summary>Applies on/off commands immediately without advancing the energy counter.</summary>
    [Fact]
    public void ApplyCommand_TurnOffAndOn_RefreshesHeaderImmediately()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, ONE_HOUR, createWeather(12.0));

        // Act
        CommandResult offResult = device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0.0));
        DeviceState offState = device.Header.State;
        int offPowerW = device.Header.ActivePowerW;
        bool wasOff = !device.Header.IsOn;
        CommandResult onResult = device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0.0));

        // Assert
        Assert.True(offResult.Accepted);
        Assert.True(onResult.Accepted);
        Assert.True(wasOff);
        Assert.Equal(DeviceState.Off, offState);
        Assert.Equal(0, offPowerW);
        Assert.True(device.Header.IsOn);
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.Equal(100000, device.Header.ActivePowerW);
        Assert.Equal(100000L, device.Header.EnergyWh);
    }

    /// <summary>Forwards the common power limit and reports the limited state immediately.</summary>
    [Fact]
    public void ApplyCommand_SetLimit_RefreshesLimitedHeader()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, TimeSpan.Zero, createWeather(12.0));

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50.0));

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(50.0, device.Header.PowerLimitPercent);
        Assert.Equal(50000, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Limited, device.Header.State);
        Assert.Equal(0L, device.Header.EnergyWh);
    }

    /// <summary>Reports a technical fault separately from a storm and preserves storm protection on reset.</summary>
    [Theory]
    [InlineData(12.0, DeviceState.Running, 100000)]
    [InlineData(25.0, DeviceState.Standby, 0)]
    public void ApplyCommand_FaultAndReset_UpdatesFaultCode(double windSpeedMs, DeviceState expectedState, int expectedPowerW)
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, TimeSpan.Zero, createWeather(windSpeedMs));

        // Act
        CommandResult faultResult = device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));
        ushort faultCode = device.Header.FaultCode;
        DeviceState faultState = device.Header.State;
        int faultPowerW = device.Header.ActivePowerW;
        CommandResult resetResult = device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 0.0));

        // Assert
        Assert.True(faultResult.Accepted);
        Assert.True(resetResult.Accepted);
        Assert.Equal((ushort)1, faultCode);
        Assert.Equal(DeviceState.Fault, faultState);
        Assert.Equal(0, faultPowerW);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(expectedState, device.Header.State);
        Assert.Equal(expectedPowerW, device.Header.ActivePowerW);
    }

    /// <summary>Restores the model and header when the shared clock starts a new run.</summary>
    [Fact]
    public void Reset_AfterOperation_RestoresInitialHeader()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, ONE_HOUR, createWeather(12.0));
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50.0));
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0.0));

        // Act
        device.reset(VIRTUAL_NOW.AddDays(1));

        // Assert
        Assert.Equal(0L, device.Header.EnergyWh);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
        Assert.True(device.Header.IsOn);
        Assert.Equal(DeviceState.Standby, device.Header.State);
    }

    /// <summary>Supplies stable display columns with units and a bar relative to rated power.</summary>
    [Fact]
    public void DisplayRows_WithRatedWind_ShowsWindPowerStatusAndEnergy()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());

        // Act
        device.update(VIRTUAL_NOW, ONE_HOUR, createWeather(12.0));

        // Assert
        Assert.Equal(6, device.DisplayRows.Count);
        Assert.Equal("100.00 kW", findRow(device, "Power").Value);
        Assert.True(findRow(device, "Power").HasBar);
        Assert.Equal(1.0, findRow(device, "Power").BarFraction);
        Assert.Equal("12.0 m/s", findRow(device, "Wind speed").Value);
        Assert.Equal("100.00 kW", findRow(device, "Available power").Value);
        Assert.Equal("Running", findRow(device, "Status").Value);
        Assert.Equal("100.00 kWh", findRow(device, "Total energy").Value);
        Assert.Equal("none", findRow(device, "Fault").Value);
    }

    /// <summary>Shows protection and unavailable weather as warnings rather than technical faults.</summary>
    [Theory]
    [InlineData(null, "Weather unavailable", "n/a")]
    [InlineData(double.NaN, "Weather unavailable", "n/a")]
    [InlineData(25.0, "Storm shutdown", "25.0 m/s")]
    public void DisplayRows_WithProtectionOrMissingWeather_ShowsWarning(double? windSpeedMs, string expectedStatus, string expectedWind)
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        WeatherDto? weather = null;
        if (windSpeedMs.HasValue)
        {
            weather = createWeather(windSpeedMs.Value);
        }

        // Act
        device.update(VIRTUAL_NOW, ONE_HOUR, weather);

        // Assert
        Assert.Equal(expectedStatus, findRow(device, "Status").Value);
        Assert.Equal(DisplaySeverity.Warning, findRow(device, "Status").Severity);
        Assert.Equal(expectedWind, findRow(device, "Wind speed").Value);
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    /// <summary>Makes a technical fault visible in both status and fault rows.</summary>
    [Fact]
    public void DisplayRows_WithTechnicalFault_MarksFaultSeverity()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));

        // Assert
        Assert.Equal(DisplaySeverity.Fault, findRow(device, "Status").Severity);
        Assert.Equal(DisplaySeverity.Fault, findRow(device, "Fault").Severity);
        Assert.Contains("Technical fault", findRow(device, "Fault").Value);
    }

    /// <summary>Exposes wind details as typed values rather than requiring formatted text parsing.</summary>
    [Fact]
    public void Update_WithRatedWind_ExposesTypedWindValues()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());

        // Act
        device.update(VIRTUAL_NOW, TimeSpan.Zero, createWeather(12.0));

        // Assert
        Assert.Equal(12.0, device.WindSpeedMs);
        Assert.Equal(100.0, device.AvailablePowerKw);
        Assert.Equal(WindPlantStatus.Running, device.OperatingStatus);
    }

    /// <summary>Retains the common device identity validation.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyDeviceId_Throws(string deviceId)
    {
        // Arrange
        WindOptions options = new WindOptions();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(delegate
        {
            new WindDevice(deviceId, options);
        });

        // Assert
        Assert.Equal("deviceId", exception.ParamName);
    }

    /// <summary>Surfaces missing wind configuration as a readable argument error.</summary>
    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        // Arrange
        WindOptions options = null!;

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(delegate
        {
            new WindDevice("wind-1", options);
        });

        // Assert
        Assert.Equal("options", exception.ParamName);
    }

    /// <summary>Rejects invalid plant parameters before starting the device.</summary>
    [Fact]
    public void Constructor_WithInvalidOptions_Throws()
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = 0.0;

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(delegate
        {
            new WindDevice("wind-1", options);
        });

        // Assert
        Assert.Equal(nameof(WindOptions.RatedPowerKw), exception.ParamName);
    }

    /// <summary>Refreshes values on a zero step without increasing cumulative energy.</summary>
    [Fact]
    public void Update_WithZeroStep_KeepsEnergy()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, ONE_HOUR, createWeather(12.0));

        // Act
        device.update(VIRTUAL_NOW, TimeSpan.Zero, createWeather(0.0));

        // Assert
        Assert.Equal(100000L, device.Header.EnergyWh);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Standby, device.Header.State);
    }

    /// <summary>Preserves the published header when a negative duration is rejected.</summary>
    [Fact]
    public void Update_WithNegativeStep_ThrowsAndKeepsHeader()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, ONE_HOUR, createWeather(12.0));

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(delegate
        {
            device.update(VIRTUAL_NOW, TimeSpan.FromSeconds(-1), null);
        });

        // Assert
        Assert.Equal("step", exception.ParamName);
        Assert.Equal(100000L, device.Header.EnergyWh);
        Assert.Equal(100000, device.Header.ActivePowerW);
    }

    /// <summary>Rejects unknown commands without changing the wind model.</summary>
    [Fact]
    public void ApplyCommand_WithUnknownName_IsRejected()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("Unknown", 0.0));

        // Assert
        Assert.False(result.Accepted);
        Assert.Contains("Unknown", result.Message);
        Assert.True(device.Header.IsOn);
    }

    /// <summary>Shows the power bar relative to plant rating even while limited.</summary>
    [Fact]
    public void DisplayRows_WithLimit_UsesRatedPowerForBar()
    {
        // Arrange
        WindDevice device = new WindDevice("wind-1", new WindOptions());
        device.update(VIRTUAL_NOW, TimeSpan.Zero, createWeather(12.0));

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50.0));

        // Assert
        Assert.Equal(0.5, findRow(device, "Power").BarFraction);
        Assert.Equal("100.00 kW", findRow(device, "Available power").Value);
        Assert.Equal("Limited", findRow(device, "Status").Value);
    }

    /// <summary>Keeps fractional watt-hours in the model instead of rounding each increment away.</summary>
    [Fact]
    public void Update_WithFractionalEnergy_RoundsOnlyCumulativeHeader()
    {
        // Arrange
        WindOptions options = new WindOptions();
        options.RatedPowerKw = 0.001;
        WindDevice device = new WindDevice("wind-1", options);
        TimeSpan quarterHour = TimeSpan.FromMinutes(15);

        // Act
        device.update(VIRTUAL_NOW, quarterHour, createWeather(12.0));
        long firstEnergyWh = device.Header.EnergyWh;
        device.update(VIRTUAL_NOW, quarterHour, createWeather(12.0));
        device.update(VIRTUAL_NOW, quarterHour, createWeather(12.0));

        // Assert
        Assert.Equal(0L, firstEnergyWh);
        Assert.Equal(1L, device.Header.EnergyWh);
        Assert.Equal(1, device.Header.ActivePowerW);
    }

    /// <summary>Finds a display column by its fixed label.</summary>
    private static DisplayRow findRow(WindDevice device, string label)
    {
        foreach (DisplayRow row in device.DisplayRows)
        {
            if (row.Label == label)
            {
                return row;
            }
        }

        throw new InvalidOperationException("Missing display row: " + label);
    }

    /// <summary>Creates fixed wind input without reading an external source.</summary>
    private static WeatherDto createWeather(double windSpeedMs)
    {
        WeatherDto weather = new WeatherDto();
        weather.WindSpeedMs = windSpeedMs;
        return weather;
    }
}
