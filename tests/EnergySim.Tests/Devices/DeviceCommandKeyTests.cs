using EnergySim.Core.Devices;
using EnergySim.Battery;
using EnergySim.Charger;
using EnergySim.Pv;
using EnergySim.Wind;

namespace EnergySim.Tests.Devices;

public class DeviceCommandKeyTests
{
    private const string UI_RESERVED_KEYS = "PV+-Q";

    public static IEnumerable<object[]> allDevices()
    {
        yield return new object[] { new PvDevice("pv-1", new PvOptions()) };
        yield return new object[] { new BatteryDevice("bat-1", new BatteryOptions()) };
        yield return new object[] { new ChargerDevice("chg-1", new ChargerOptions()) };
        yield return new object[] { new WindDevice("wind-1", new WindOptions()) };
    }

    private static CommandDescriptor findByName(ISimulatedDevice device, string name)
    {
        foreach (CommandDescriptor command in device.Commands)
        {
            if (command.Name == name)
            {
                return command;
            }
        }

        throw new InvalidOperationException("Command not offered: " + name);
    }

    [Theory]
    [MemberData(nameof(allDevices))]
    public void Commands_CommonCommands_UseTheSameKeysOnEveryDevice(ISimulatedDevice device)
    {
        // Assert
        Assert.Equal('O', findByName(device, CommonCommands.TURN_ON).Key);
        Assert.Equal('F', findByName(device, CommonCommands.TURN_OFF).Key);
        Assert.Equal('L', findByName(device, CommonCommands.SET_LIMIT).Key);
        Assert.Equal('T', findByName(device, CommonCommands.TRIGGER_FAULT).Key);
        Assert.Equal('R', findByName(device, CommonCommands.RESET_FAULT).Key);
        Assert.True(findByName(device, CommonCommands.SET_LIMIT).HasParameter);
        Assert.Equal(100.0, findByName(device, CommonCommands.SET_LIMIT).Maximum);
    }

    [Theory]
    [MemberData(nameof(allDevices))]
    public void Commands_Keys_AreUniqueAndNotUiReserved(ISimulatedDevice device)
    {
        // Arrange
        HashSet<char> seen = new HashSet<char>();

        // Act + Assert
        foreach (CommandDescriptor command in device.Commands)
        {
            char key = char.ToUpperInvariant(command.Key);
            Assert.DoesNotContain(key, UI_RESERVED_KEYS);
            Assert.True(seen.Add(key), "duplicate key " + key);
        }
    }

    [Fact]
    public void Commands_DeviceSpecific_UseAgreedKeys()
    {
        // Arrange
        BatteryDevice battery = new BatteryDevice("bat-1", new BatteryOptions());
        ChargerDevice charger = new ChargerDevice("chg-1", new ChargerOptions());

        // Assert
        Assert.Equal('A', findByName(battery, BatteryDevice.COMMAND_AUTO_MODE).Key);
        Assert.Equal('M', findByName(battery, BatteryDevice.COMMAND_SET_POWER_KW).Key);
        Assert.Equal('C', findByName(charger, ChargerDevice.CONNECT_CAR).Key);
        Assert.Equal('D', findByName(charger, ChargerDevice.DISCONNECT_CAR).Key);
        Assert.Equal('N', findByName(charger, ChargerDevice.FAULT_PORT).Key);
        Assert.Equal('E', findByName(charger, ChargerDevice.CLEAR_PORT_FAULT).Key);
    }

    [Theory]
    [MemberData(nameof(allDevices))]
    public void ApplyCommand_SetLimitOutOfRange_IsRejectedAndKeepsLimit(ISimulatedDevice device)
    {
        // Act
        CommandResult tooHigh = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 101.0));
        CommandResult negative = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, -1.0));

        // Assert
        Assert.False(tooHigh.Accepted);
        Assert.False(negative.Accepted);
        Assert.Equal(100.0, device.Header.PowerLimitPercent);
    }

    [Theory]
    [MemberData(nameof(allDevices))]
    public void ApplyCommand_TurnOffAndOn_ChangesIsOn(ISimulatedDevice device)
    {
        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0.0));
        bool isOnAfterOff = device.Header.IsOn;
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0.0));

        // Assert
        Assert.False(isOnAfterOff);
        Assert.True(device.Header.IsOn);
    }

    [Theory]
    [MemberData(nameof(allDevices))]
    public void ApplyCommand_TriggerThenResetFault_ShowsAndClearsFaultCode(ISimulatedDevice device)
    {
        // Act
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0.0));
        ushort faultCode = device.Header.FaultCode;
        device.update(new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero), TimeSpan.FromHours(3), null); // battery must cool down first
        device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 0.0));

        // Assert
        Assert.NotEqual((ushort)0, faultCode);
        Assert.Equal((ushort)0, device.Header.FaultCode);
    }
}
