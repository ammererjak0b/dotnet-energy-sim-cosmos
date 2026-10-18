using EnergySim.Core.Devices;
using EnergySim.Charger;

namespace EnergySim.Tests.Charger;

public class ChargerCommandTests
{
    [Fact]
    public void Commands_Keys_AreDistinct()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        HashSet<char> keys = new HashSet<char>();
        HashSet<string> names = new HashSet<string>();

        // Act
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            keys.Add(descriptor.Key);
            names.Add(descriptor.Name);
        }

        // Assert
        Assert.Equal(device.Commands.Count, keys.Count);
        Assert.Equal(device.Commands.Count, names.Count);
        Assert.Contains(CommonCommands.TURN_ON, names);
        Assert.Contains(CommonCommands.TURN_OFF, names);
        Assert.Contains(CommonCommands.SET_LIMIT, names);
        Assert.Contains(CommonCommands.RESET_FAULT, names);
        Assert.Contains(CommonCommands.TRIGGER_FAULT, names);
        Assert.Contains("ConnectCar", names);
        Assert.Contains("DisconnectCar", names);
        Assert.Contains("FaultPort", names);
        Assert.Contains("ClearPortFault", names);
    }

    [Fact]
    public void Commands_PortCommands_DescribePortRange()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandDescriptor? connect = null;
        foreach (CommandDescriptor descriptor in device.Commands)
        {
            if (descriptor.Name == "ConnectCar")
            {
                connect = descriptor;
            }
        }

        // Assert
        Assert.NotNull(connect);
        Assert.True(connect.HasParameter);
        Assert.Equal(1.0, connect.Minimum);
        Assert.Equal(2.0, connect.Maximum);
    }

    [Fact]
    public void ApplyCommand_TurnOffAndOn_SetsIsOn()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult off = device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));
        bool isOnAfterOff = device.Header.IsOn;
        CommandResult on = device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0));

        // Assert
        Assert.True(off.Accepted);
        Assert.False(isOnAfterOff);
        Assert.True(on.Accepted);
        Assert.True(device.Header.IsOn);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(100.5, false)]
    public void ApplyCommand_SetLimit_ValidatesRange(double value, bool expectedAccepted)
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, value));

        // Assert
        Assert.Equal(expectedAccepted, result.Accepted);
        if (expectedAccepted)
        {
            Assert.Equal(value, device.Header.PowerLimitPercent, 6);
        }
        else
        {
            Assert.Equal(100.0, device.Header.PowerLimitPercent, 6);
        }
    }

    [Fact]
    public void ApplyCommand_TriggerAndResetFault_ClearsChargerAndPortFaults()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));
        device.applyCommand(new DeviceCommand("FaultPort", 1));
        ushort codeWithFault = device.Header.FaultCode;

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(CommonCommands.RESET_FAULT, 1));

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(ChargerDevice.FAULT_CODE_CHARGER, codeWithFault);
        Assert.Equal(0, device.Header.FaultCode);
        Assert.Equal(DeviceState.Standby, device.Header.State);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ApplyCommand_ConnectCar_CreatesDefaultCar(int port)
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("ConnectCar", port));
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Assert
        Assert.True(result.Accepted);
        string socRegister = "Port" + port + "SocPercent";
        Assert.Equal(60.31, ChargerTestSupport.getValue(device, socRegister), 2);
        Assert.Equal(-11000, device.Header.ActivePowerW);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(1.5)]
    public void ApplyCommand_ConnectCarInvalidPort_IsRejected(double port)
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("ConnectCar", port));

        // Assert
        Assert.False(result.Accepted);
    }

    [Fact]
    public void ApplyCommand_ConnectCarOnOccupiedPort_IsRejected()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.applyCommand(new DeviceCommand("ConnectCar", 1));

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("ConnectCar", 1));

        // Assert
        Assert.False(result.Accepted);
    }

    [Fact]
    public void ApplyCommand_DisconnectCar_FreesPort()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.applyCommand(new DeviceCommand("ConnectCar", 2));
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("DisconnectCar", 2));

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2SocPercent"), 6);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2Status"), 6);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void ApplyCommand_DisconnectCarOnEmptyPort_IsRejected()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("DisconnectCar", 1));

        // Assert
        Assert.False(result.Accepted);
    }

    [Fact]
    public void ApplyCommand_FaultPortAndClear_TogglesPortFault()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult faultResult = device.applyCommand(new DeviceCommand("FaultPort", 2));
        double statusWithFault = ChargerTestSupport.getValue(device, "Port2Status");
        CommandResult clearResult = device.applyCommand(new DeviceCommand("ClearPortFault", 2));

        // Assert
        Assert.True(faultResult.Accepted);
        Assert.Equal(2.0, statusWithFault, 6);
        Assert.True(clearResult.Accepted);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2Status"), 6);
        Assert.Equal(0, device.Header.FaultCode);
    }

    [Theory]
    [InlineData("FaultPort", 0)]
    [InlineData("FaultPort", 3)]
    [InlineData("ClearPortFault", 5)]
    [InlineData("DisconnectCar", 0)]
    public void ApplyCommand_PortCommandInvalidPort_IsRejected(string name, double port)
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand(name, port));

        // Assert
        Assert.False(result.Accepted);
    }

    [Fact]
    public void ApplyCommand_UnknownCommand_IsRejected()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        CommandResult result = device.applyCommand(new DeviceCommand("Explode", 1));

        // Assert
        Assert.False(result.Accepted);
    }
}
