using EnergySim.Core.Devices;
using EnergySim.Battery;

namespace EnergySim.Tests.Battery;

public class BatteryDeviceTests
{
    private static readonly DateTimeOffset NIGHT = new DateTimeOffset(2026, 7, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MORNING = new DateTimeOffset(2026, 7, 15, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ONE_MINUTE = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ONE_HOUR = TimeSpan.FromHours(1);

    private static BatteryDevice createDevice()
    {
        return new BatteryDevice("bat-1", new BatteryOptions());
    }

    private static double getRegister(BatteryDevice device, string name)
    {
        if (name == "SocPercent")
        {
            return device.SocPercent;
        }

        if (name == "CellTemperatureC")
        {
            return device.TemperatureC;
        }

        if (name == "ChargedEnergyWh")
        {
            return Math.Round(device.ChargedEnergyWh);
        }

        if (name == "DischargedEnergyWh")
        {
            return device.Header.EnergyWh;
        }

        throw new InvalidOperationException("Unknown value: " + name);
    }

    private static void setManualPower(BatteryDevice device, double powerKw)
    {
        CommandResult result = device.applyCommand(new DeviceCommand("SetPowerKw", powerKw));
        Assert.True(result.Accepted, result.Message);
    }

    [Fact]
    public void Constructor_SetsIdentityAndInitialHeader()
    {
        // Arrange + Act
        BatteryDevice device = createDevice();

        // Assert
        Assert.Equal("bat-1", device.DeviceId);
        Assert.Equal(DeviceType.Battery, device.Type);
        Assert.False(device.NeedsWeather);
        Assert.True(device.Header.IsOn);
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(0, device.Header.FaultCode);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentException>(() => new BatteryDevice("", new BatteryOptions()));
        Assert.Throws<ArgumentException>(() => new BatteryDevice("  ", new BatteryOptions()));
        Assert.Throws<ArgumentNullException>(() => new BatteryDevice("bat-1", null!));
        BatteryOptions invalid = new BatteryOptions();
        invalid.CapacityKWh = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => new BatteryDevice("bat-1", invalid));
    }

    [Fact]
    public void Update_AutoAtNight_ChargesWithNegativePower()
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.True(device.Header.ActivePowerW < 0);
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.Equal(0, device.Header.EnergyWh); // EnergyWh = discharged side only
        Assert.True(getRegister(device, "ChargedEnergyWh") > 0);
    }

    [Fact]
    public void Update_AutoInMorning_DischargesWithPositivePower()
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        device.update(MORNING, ONE_MINUTE, null);

        // Assert
        Assert.True(device.Header.ActivePowerW > 0);
        Assert.True(device.Header.EnergyWh > 0);
        Assert.Equal(device.Header.EnergyWh, getRegister(device, "DischargedEnergyWh"));
    }

    [Fact]
    public void Update_AutoSameVirtualTime_GivesSamePowerRegardlessOfCallCount()
    {
        // Arrange
        BatteryDevice once = createDevice();
        BatteryDevice often = createDevice();
        TimeSpan tinyStep = TimeSpan.FromMilliseconds(1);

        // Act
        once.update(MORNING, tinyStep, null);
        for (int call = 0; call < 50; call++)
        {
            often.update(MORNING, tinyStep, null);
        }

        // Assert
        Assert.Equal(once.Header.ActivePowerW, often.Header.ActivePowerW);
    }

    [Fact]
    public void Update_ZeroStep_ChangesNothing()
    {
        // Arrange
        BatteryDevice device = createDevice();
        device.update(MORNING, ONE_MINUTE, null);
        int powerBefore = device.Header.ActivePowerW;
        long energyBefore = device.Header.EnergyWh;
        double socBefore = getRegister(device, "SocPercent");
        double temperatureBefore = getRegister(device, "CellTemperatureC");

        // Act
        device.update(MORNING.AddHours(5), TimeSpan.Zero, null);

        // Assert
        Assert.Equal(powerBefore, device.Header.ActivePowerW);
        Assert.Equal(energyBefore, device.Header.EnergyWh);
        Assert.Equal(socBefore, getRegister(device, "SocPercent"));
        Assert.Equal(temperatureBefore, getRegister(device, "CellTemperatureC"));
    }

    [Fact]
    public void Update_NegativeStep_Throws()
    {
        BatteryDevice device = createDevice();

        Assert.Throws<ArgumentOutOfRangeException>(() => device.update(NIGHT, TimeSpan.FromSeconds(-1), null));
    }

    [Fact]
    public void ApplyCommand_SetPowerKw_SwitchesToManual()
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        setManualPower(device, 20.0);
        device.update(NIGHT, ONE_MINUTE, null); // night would charge in auto

        // Assert
        Assert.Equal(20000, device.Header.ActivePowerW);
    }

    [Fact]
    public void ApplyCommand_SetPowerKwNegative_Charges()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, -30.0);

        // Act
        device.update(MORNING, ONE_MINUTE, null);

        // Assert
        Assert.Equal(-30000, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Running, device.Header.State);
    }

    [Theory]
    [InlineData(50.1)]
    [InlineData(-50.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ApplyCommand_SetPowerKwOutOfRange_IsRejected(double powerKw)
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("SetPowerKw", powerKw));

        // Assert
        Assert.False(result.Accepted);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void ApplyCommand_AutoMode_ReturnsToSchedule()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 20.0);

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("AutoMode", 0));
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.True(result.Accepted);
        Assert.True(device.Header.ActivePowerW < 0);
    }

    [Fact]
    public void ApplyCommand_TurnOff_StopsPowerAndShowsOff()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 20.0);
        device.update(NIGHT, ONE_MINUTE, null);

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));
        device.update(NIGHT.AddMinutes(1), ONE_MINUTE, null);

        // Assert
        Assert.False(device.Header.IsOn);
        Assert.Equal(DeviceState.Off, device.Header.State);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void ApplyCommand_TurnOn_ResumesOperation()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 20.0);
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0));
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.True(device.Header.IsOn);
        Assert.Equal(20000, device.Header.ActivePowerW);
    }

    [Fact]
    public void ApplyCommand_SetLimit_CurtailsPowerAndShowsLimited()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 40.0);

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50.0));
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(50.0, device.Header.PowerLimitPercent);
        Assert.Equal(25000, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Limited, device.Header.State);
    }

    [Fact]
    public void Update_LimitNotReached_StaysRunning()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 10.0);
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50.0));

        // Act
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.Equal(DeviceState.Running, device.Header.State);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(100.5)]
    [InlineData(double.NaN)]
    public void ApplyCommand_SetLimitOutOfRange_IsRejected(double percent)
    {
        BatteryDevice device = createDevice();

        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, percent));

        Assert.False(result.Accepted);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
    }

    [Fact]
    public void ApplyCommand_TriggerFault_SetsFaultState()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 20.0);

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        Assert.Equal(1, device.Header.FaultCode);
        Assert.Equal(DeviceState.Fault, device.Header.State);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void ApplyCommand_ResetFaultWhileHot_IsRejected()
    {
        // Arrange
        BatteryDevice device = createDevice();
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 0));

        // Assert
        Assert.False(result.Accepted);
        Assert.Equal(1, device.Header.FaultCode);
    }

    [Fact]
    public void ApplyCommand_ResetFaultAfterCooling_ClearsFault()
    {
        // Arrange
        BatteryDevice device = createDevice();
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));
        device.update(NIGHT, ONE_HOUR, null); // cools to 37 degC

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 0));

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(0, device.Header.FaultCode);
        Assert.NotEqual(DeviceState.Fault, device.Header.State);
    }

    [Fact]
    public void ApplyCommand_UnknownCommand_IsRejected()
    {
        BatteryDevice device = createDevice();

        CommandResult result = device.applyCommand(new DeviceCommand("Explode", 1));

        Assert.False(result.Accepted);
        Assert.Contains("Explode", result.Message);
    }

    [Fact]
    public void Commands_HaveDistinctNamesAndKeys()
    {
        // Arrange
        BatteryDevice device = createDevice();
        HashSet<string> names = new HashSet<string>();
        HashSet<char> keys = new HashSet<char>();

        // Act
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            names.Add(descriptor.Name);
            keys.Add(char.ToUpperInvariant(descriptor.Key));
        }

        // Assert
        Assert.Equal(device.Commands.Count, names.Count);
        Assert.Equal(device.Commands.Count, keys.Count);
        Assert.Contains(CommonCommands.TURN_ON, names);
        Assert.Contains(CommonCommands.TURN_OFF, names);
        Assert.Contains(CommonCommands.SET_LIMIT, names);
        Assert.Contains(CommonCommands.RESET_FAULT, names);
        Assert.Contains(CommonCommands.TRIGGER_FAULT, names);
        Assert.Contains("AutoMode", names);
        Assert.Contains("SetPowerKw", names);
    }

    [Fact]
    public void Commands_SetPowerKw_HasRangeFromOptions()
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        CommandDescriptor? setPower = null;
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            if (descriptor.Name == "SetPowerKw")
            {
                setPower = descriptor;
            }
        }

        // Assert
        Assert.NotNull(setPower);
        Assert.True(setPower.HasParameter);
        Assert.Equal(-50.0, setPower.Minimum);
        Assert.Equal(50.0, setPower.Maximum);
        Assert.Equal("kW", setPower.Unit);
    }

    [Fact]
    public void SpecificValues_InitialState_MatchesOptions()
    {
        BatteryDevice device = createDevice();

        Assert.Equal(50.0, getRegister(device, "SocPercent"), 6);
        Assert.Equal(25.0, getRegister(device, "CellTemperatureC"), 6);
        Assert.Equal(0.0, getRegister(device, "ChargedEnergyWh"));
        Assert.Equal(0.0, getRegister(device, "DischargedEnergyWh"));
    }

    [Fact]
    public void SpecificValues_AfterCharging_ShowChargedWh()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, -10.0);

        // Act
        device.update(NIGHT, ONE_HOUR, null);

        // Assert
        Assert.Equal(10000.0, getRegister(device, "ChargedEnergyWh"));
        Assert.Equal(59.5, getRegister(device, "SocPercent"), 6);
        Assert.Equal(0, device.Header.EnergyWh);
    }

    [Fact]
    public void DisplayRows_ContainBarsForPowerAndSoc()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 25.0);

        // Act
        device.update(NIGHT, ONE_MINUTE, null);

        // Assert
        DisplayRow? power = findRow(device, "Power");
        DisplayRow? soc = findRow(device, "SoC");
        Assert.NotNull(power);
        Assert.NotNull(soc);
        Assert.True(power.HasBar);
        Assert.Equal(0.5, power.BarFraction, 3);
        Assert.True(soc.HasBar);
        Assert.InRange(soc.BarFraction, 0.49, 0.5);
        Assert.NotNull(findRow(device, "Temperature"));
        Assert.NotNull(findRow(device, "Charged"));
        Assert.NotNull(findRow(device, "Discharged"));
        Assert.NotNull(findRow(device, "Mode"));
        Assert.NotNull(findRow(device, "Fault"));
    }

    [Fact]
    public void DisplayRows_Fault_ShowsFaultSeverity()
    {
        // Arrange
        BatteryDevice device = createDevice();

        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));

        // Assert
        DisplayRow? fault = findRow(device, "Fault");
        Assert.NotNull(fault);
        Assert.Equal(DisplaySeverity.Fault, fault.Severity);
        Assert.Contains("Overtemperature", fault.Value);
    }

    [Fact]
    public void DisplayRows_Mode_ShowsAutoOrManual()
    {
        BatteryDevice device = createDevice();
        Assert.Equal("Auto", findRow(device, "Mode")!.Value);

        setManualPower(device, 5.0);

        Assert.Equal("Manual", findRow(device, "Mode")!.Value);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        BatteryDevice device = createDevice();
        setManualPower(device, 30.0);
        device.update(NIGHT, ONE_HOUR, null);
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 30.0));
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));

        // Act
        device.reset(MORNING);

        // Assert
        Assert.Equal(50.0, getRegister(device, "SocPercent"), 6);
        Assert.Equal(25.0, getRegister(device, "CellTemperatureC"), 6);
        Assert.Equal(0.0, getRegister(device, "DischargedEnergyWh"));
        Assert.Equal(0, device.Header.EnergyWh);
        Assert.Equal(0, device.Header.FaultCode);
        Assert.True(device.Header.IsOn);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal("Auto", findRow(device, "Mode")!.Value);
    }

    private static DisplayRow? findRow(BatteryDevice device, string label)
    {
        foreach (DisplayRow row in device.DisplayRows)
        {
            if (row.Label == label)
            {
                return row;
            }
        }

        return null;
    }
}
