using EnergySim.Core.Devices;
using EnergySim.Core.Weather;
using EnergySim.Weather;
using EnergySim.Pv;

namespace EnergySim.Tests.Pv;

public class PvDeviceTests
{
    private static PvDevice createDevice()
    {
        return new PvDevice("pv-1", new PvOptions());
    }

    private static double getDailyEnergyWh(PvDevice device)
    {
        return Math.Round(device.DailyEnergyWh);
    }

    private static string getRow(PvDevice device, string label)
    {
        foreach (DisplayRow row in device.DisplayRows)
        {
            if (row.Label == label)
            {
                return row.Value;
            }
        }

        throw new InvalidOperationException("Row not found: " + label);
    }

    [Fact]
    public void Constructor_SetsIdentity()
    {
        // Arrange, Act
        PvDevice device = createDevice();

        // Assert
        Assert.Equal("pv-1", device.DeviceId);
        Assert.Equal(DeviceType.Pv, device.Type);
        Assert.True(device.NeedsWeather);
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.True(device.Header.IsOn);
    }

    [Fact]
    public void Update_AtMidday_FillsHeaderRunning()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        device.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.Equal(3008, device.Header.ActivePowerW);
        Assert.Equal(3008L, device.Header.EnergyWh);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
    }

    [Fact]
    public void Update_AtNight_IsStandbyWithZeroPower()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createNightWeather());

        // Assert
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_WithoutWeather_IsStandbyWithoutFault()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        device.update(PvTestData.NOON, PvTestData.STEP, null);

        // Assert
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal("n/a", getRow(device, "Irradiance"));
    }

    [Fact]
    public void Update_WithZeroStep_KeepsEnergy()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());

        // Act
        device.update(PvTestData.NOON, TimeSpan.Zero, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(3008L, device.Header.EnergyWh);
    }

    [Fact]
    public void Update_AtUtcMidnight_ResetsDailyEnergyButNotHeaderEnergy()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());
        double dailyBefore = getDailyEnergyWh(device);

        // Act
        device.update(new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero), TimeSpan.Zero, PvTestData.createNightWeather());

        // Assert
        Assert.Equal(3008.0, dailyBefore);
        Assert.Equal(0.0, getDailyEnergyWh(device));
        Assert.Equal(3008L, device.Header.EnergyWh);
    }

    [Fact]
    public void DisplayRows_ShowPowerBarAgainstPeak()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        DisplayRow powerRow = device.DisplayRows[0];
        Assert.Equal("Power", powerRow.Label);
        Assert.True(powerRow.HasBar);
        Assert.Equal(3.008 / 5.0, powerRow.BarFraction, 6);
        Assert.Equal("3.01 kW", powerRow.Value);
    }

    [Fact]
    public void DisplayRows_OnFault_MarkFaultRow()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));

        // Assert
        DisplayRow faultRow = device.DisplayRows[device.DisplayRows.Count - 1];
        Assert.Equal("Fault", faultRow.Label);
        Assert.Equal(DisplaySeverity.Fault, faultRow.Severity);
    }

    [Fact]
    public void Commands_OfferAllCommonCommandsWithDistinctKeys()
    {
        // Arrange, Act
        PvDevice device = createDevice();

        // Assert
        List<char> keys = new List<char>();
        List<string> names = new List<string>();
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            keys.Add(descriptor.Key);
            names.Add(descriptor.Name);
        }

        Assert.Equal(keys.Count, new HashSet<char>(keys).Count);
        Assert.Contains(CommonCommands.TURN_ON, names);
        Assert.Contains(CommonCommands.TURN_OFF, names);
        Assert.Contains(CommonCommands.SET_LIMIT, names);
        Assert.Contains(CommonCommands.TRIGGER_FAULT, names);
        Assert.Contains(CommonCommands.RESET_FAULT, names);
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            if (descriptor.Name == CommonCommands.SET_LIMIT)
            {
                Assert.True(descriptor.HasParameter);
                Assert.Equal(0.0, descriptor.Minimum);
                Assert.Equal(100.0, descriptor.Maximum);
            }
        }
    }

    [Fact]
    public void ApplyCommand_SetLimit_CapsPowerAndShowsLimited()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 10.0));

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(DeviceState.Limited, device.Header.State);
        Assert.Equal(500, device.Header.ActivePowerW);
        Assert.Equal(10.0, device.Header.PowerLimitPercent);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(100.5)]
    [InlineData(double.NaN)]
    public void ApplyCommand_SetLimitOutOfRange_IsRejectedWithMessage(double value)
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, value));

        // Assert
        Assert.False(result.Accepted);
        Assert.Contains("0", result.Message);
        Assert.Contains("100", result.Message);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
    }

    [Fact]
    public void ApplyCommand_FaultAndReset_ChangesStateAndFaultCode()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Act
        CommandResult faultResult = device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));
        DeviceState faultState = device.Header.State;
        ushort faultCode = device.Header.FaultCode;
        int faultPowerW = device.Header.ActivePowerW;
        CommandResult resetResult = device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 1.0));
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.True(faultResult.Accepted);
        Assert.Equal(DeviceState.Fault, faultState);
        Assert.Equal((ushort)1, faultCode);
        Assert.Equal(0, faultPowerW);
        Assert.True(resetResult.Accepted);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(DeviceState.Running, device.Header.State);
    }

    [Fact]
    public void ApplyCommand_TurnOffAndOn_SwitchesState()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0.0));
        DeviceState offState = device.Header.State;
        bool offIsOn = device.Header.IsOn;
        int offPowerW = device.Header.ActivePowerW;
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0.0));
        device.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(DeviceState.Off, offState);
        Assert.False(offIsOn);
        Assert.Equal(0, offPowerW);
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.True(device.Header.IsOn);
    }

    [Fact]
    public void ApplyCommand_Unknown_IsRejected()
    {
        // Arrange
        PvDevice device = createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("SelfDestruct", 0.0));

        // Assert
        Assert.False(result.Accepted);
        Assert.Contains("SelfDestruct", result.Message);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        PvDevice device = createDevice();
        device.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 30.0));
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));

        // Act
        device.reset(PvTestData.NOON);

        // Assert
        Assert.Equal(0L, device.Header.EnergyWh);
        Assert.Equal((ushort)0, device.Header.FaultCode);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(0.0, getDailyEnergyWh(device));
        Assert.Equal(DeviceState.Standby, device.Header.State);
    }
}
