using EnergySim.Modbus;

namespace EnergySim.Tests.Modbus;

/// <summary>Register encoding: big-endian per register, high word first.</summary>
public sealed class RegisterCodecTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(-123456)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void WriteInt32_ThenRead_ReturnsSameValue(int value)
    {
        // Arrange
        ushort[] registers = new ushort[4];

        // Act
        RegisterCodec.writeInt32(registers, 1, value);
        int result = RegisterCodec.readInt32(registers, 1);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void WriteInt32_Negative_UsesTwosComplementHighWordFirst()
    {
        // Arrange
        ushort[] registers = new ushort[2];

        // Act
        RegisterCodec.writeInt32(registers, 0, -2);

        // Assert
        Assert.Equal(0xFFFF, registers[0]);
        Assert.Equal(0xFFFE, registers[1]);
    }

    [Fact]
    public void WriteInt32_Min_SetsOnlySignBit()
    {
        // Arrange
        ushort[] registers = new ushort[2];

        // Act
        RegisterCodec.writeInt32(registers, 0, int.MinValue);

        // Assert
        Assert.Equal(0x8000, registers[0]);
        Assert.Equal(0x0000, registers[1]);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(65536u)]
    [InlineData(uint.MaxValue)]
    public void WriteUInt32_ThenRead_ReturnsSameValue(uint value)
    {
        // Arrange
        ushort[] registers = new ushort[2];

        // Act
        RegisterCodec.writeUInt32(registers, 0, value);
        uint result = RegisterCodec.readUInt32(registers, 0);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void WriteUInt32_Value_PutsHighWordFirst()
    {
        // Arrange
        ushort[] registers = new ushort[3];

        // Act
        RegisterCodec.writeUInt32(registers, 1, 0x12345678u);

        // Assert
        Assert.Equal(0, registers[0]);
        Assert.Equal(0x1234, registers[1]);
        Assert.Equal(0x5678, registers[2]);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(1767225600000UL)]
    [InlineData(ulong.MaxValue)]
    public void WriteUInt64_ThenRead_ReturnsSameValue(ulong value)
    {
        // Arrange
        ushort[] registers = new ushort[4];

        // Act
        RegisterCodec.writeUInt64(registers, 0, value);
        ulong result = RegisterCodec.readUInt64(registers, 0);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void WriteUInt64_Value_PutsHighWordFirst()
    {
        // Arrange
        ushort[] registers = new ushort[4];

        // Act
        RegisterCodec.writeUInt64(registers, 0, 0x0102030405060708UL);

        // Assert
        Assert.Equal(0x0102, registers[0]);
        Assert.Equal(0x0304, registers[1]);
        Assert.Equal(0x0506, registers[2]);
        Assert.Equal(0x0708, registers[3]);
    }

    [Theory]
    [InlineData(12.34, 10, 123)]
    [InlineData(-12.36, 10, -124)]
    [InlineData(5.0, 1, 5)]
    [InlineData(0.0, 10, 0)]
    public void ScaleToRaw_NormalValue_ReturnsRoundedProduct(double value, int scale, int expected)
    {
        // Act
        int raw = RegisterCodec.scaleToRaw(value, scale);

        // Assert
        Assert.Equal(expected, raw);
    }

    [Theory]
    [InlineData(1e12, int.MaxValue)]
    [InlineData(-1e12, int.MinValue)]
    [InlineData(double.PositiveInfinity, int.MaxValue)]
    [InlineData(double.NegativeInfinity, int.MinValue)]
    public void ScaleToRaw_OutOfRange_ClampsToInt32Limits(double value, int expected)
    {
        // Act
        int raw = RegisterCodec.scaleToRaw(value, 10);

        // Assert
        Assert.Equal(expected, raw);
    }

    [Fact]
    public void ScaleToRaw_NaN_ReturnsZero()
    {
        // Act
        int raw = RegisterCodec.scaleToRaw(double.NaN, 10);

        // Assert
        Assert.Equal(0, raw);
    }

    [Theory]
    [InlineData(123, 10, 12.3)]
    [InlineData(-5, 1, -5.0)]
    [InlineData(0, 10, 0.0)]
    public void RawToScaled_Value_DividesByScale(int raw, int scale, double expected)
    {
        // Act
        double value = RegisterCodec.rawToScaled(raw, scale);

        // Assert
        Assert.Equal(expected, value, 9);
    }
}
