using EnergySim.Core.Devices;
using EnergySim.Modbus;

namespace EnergySim.Tests.Modbus;

public sealed class DeviceModbusOptionsTests
{
    private static DeviceModbusOptions createValidOptions()
    {
        DeviceModbusOptions options = new DeviceModbusOptions();
        options.Port = 1511;
        options.DeviceId = "pv-1";
        return options;
    }

    [Fact]
    public void Validate_ValidOptions_DoesNotThrow()
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();

        // Act
        Exception? exception = Record.Exception(delegate { options.validate(); });

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1501)]
    [InlineData(65536)]
    public void Validate_PortOutOfRange_Throws(int port)
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.Port = port;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(validate);
    }

    [Theory]
    [InlineData(1502)]
    [InlineData(65535)]
    public void Validate_PortAtLimit_DoesNotThrow(int port)
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.Port = port;

        // Act
        Exception? exception = Record.Exception(delegate { options.validate(); });

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("localhost")]
    public void Validate_BadHost_Throws(string host)
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.Host = host;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(248)]
    public void Validate_UnitIdOutOfRange_Throws(int unitId)
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.UnitId = (byte)unitId;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(validate);
    }

    [Fact]
    public void Validate_EmptyDeviceId_Throws()
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.DeviceId = string.Empty;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }

    [Fact]
    public void Validate_UndefinedDeviceType_Throws()
    {
        // Arrange
        DeviceModbusOptions options = createValidOptions();
        options.DeviceType = (DeviceType)99;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(validate);
    }
}
