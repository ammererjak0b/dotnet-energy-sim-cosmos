using EnergySim.Modbus;

namespace EnergySim.UnitTests.Engine;

/// <summary>Registration write: port + device id (new) or port alone (old).</summary>
public sealed class ClockRegistrationCodecTests
{
    [Fact]
    public void EncodeDecode_PortAndId_RoundTrips()
    {
        // Arrange
        int[] registers = ClockRegistrationCodec.encode(1604, "wind-1");

        // Act
        ClockRegistration registration = ClockRegistrationCodec.decode(registers);

        // Assert
        Assert.Equal(ClockRegistrationCodec.REGISTER_COUNT, registers.Length);
        Assert.Equal(1604, registration.Port);
        Assert.Equal("wind-1", registration.DeviceId);
    }

    [Fact]
    public void Decode_OldSingleRegisterForm_HasPortAndNoId()
    {
        // Arrange
        int[] registers = new int[] { 1601 };

        // Act
        ClockRegistration registration = ClockRegistrationCodec.decode(registers);

        // Assert
        Assert.Equal(1601, registration.Port);
        Assert.Equal(string.Empty, registration.DeviceId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(18)]
    [InlineData(20)]
    public void Decode_WrongLength_Throws(int count)
    {
        // Arrange
        int[] registers = new int[count];

        // Act
        Action decode = delegate { ClockRegistrationCodec.decode(registers); };

        // Assert
        Assert.Throws<ArgumentException>(decode);
    }

    [Fact]
    public void Decode_BrokenIdentity_Throws()
    {
        // Arrange
        int[] registers = ClockRegistrationCodec.encode(1604, "wind-1");
        registers[1] = 99; // wrong identity version

        // Act
        Action decode = delegate { ClockRegistrationCodec.decode(registers); };

        // Assert
        Assert.Throws<ArgumentException>(decode);
    }

    [Fact]
    public void Encode_InvalidId_Throws()
    {
        // Act
        Action encode = delegate { ClockRegistrationCodec.encode(1604, "bad id!"); };

        // Assert
        Assert.Throws<ArgumentException>(encode);
    }
}
