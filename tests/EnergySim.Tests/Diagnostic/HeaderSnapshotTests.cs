using EnergySim.Core.Devices;
using EnergySim.Diagnostic;
using EnergySim.Modbus;

namespace EnergySim.Tests.Diagnostic;

public sealed class HeaderSnapshotTests
{
    private static readonly DateTimeOffset VIRTUAL_TIME = new DateTimeOffset(2026, 6, 21, 12, 30, 15, TimeSpan.Zero);

    private static ushort[] createRaw(ushort warnFlags, int activePowerW)
    {
        ushort[] raw = new ushort[RegisterMap.HEADER_REGISTER_COUNT];
        raw[RegisterMap.IN_DEVICE_TYPE] = (ushort)DeviceType.Battery;
        raw[RegisterMap.IN_MAP_VERSION] = RegisterMap.MAP_VERSION;
        raw[RegisterMap.IN_STATE] = (ushort)DeviceState.Limited;
        raw[RegisterMap.IN_FAULT_CODE] = 7;
        raw[RegisterMap.IN_WARN_FLAGS] = warnFlags;
        RegisterCodec.writeInt32(raw, RegisterMap.IN_ACTIVE_POWER_W, activePowerW);
        RegisterCodec.writeUInt32(raw, RegisterMap.IN_ENERGY_WH, 123456);
        RegisterCodec.writeUInt64(raw, RegisterMap.IN_VIRTUAL_TIME_MS, (ulong)VIRTUAL_TIME.ToUnixTimeMilliseconds());
        RegisterCodec.writeUInt32(raw, RegisterMap.IN_SEQUENCE, 70000);
        raw[RegisterMap.IN_GENERATION] = 3;
        raw[RegisterMap.IN_CLOCK_STATE] = (ushort)ClockStateCode.Paused;
        return raw;
    }

    [Fact]
    public void Decode_KnownRegisters_ReturnsAllFields()
    {
        // Arrange
        ushort[] raw = createRaw(0, -2500);

        // Act
        HeaderSnapshot header = HeaderSnapshot.decode(raw);

        // Assert
        Assert.Equal(DeviceType.Battery, header.Type);
        Assert.Equal(DeviceState.Limited, header.State);
        Assert.Equal("Limited", header.State.ToString());
        Assert.Equal(7, header.FaultCode);
        Assert.Equal(-2500, header.ActivePowerW);
        Assert.Equal(123456u, header.EnergyWh);
        Assert.Equal(VIRTUAL_TIME, header.VirtualTime);
        Assert.Equal("2026-06-21T12:30:15.0000000+00:00", header.VirtualTime.ToString("o"));
        Assert.Equal(70000u, header.Sequence);
        Assert.Equal(3, header.Generation);
        Assert.Equal(ClockStateCode.Paused, header.ClockState);
    }

    [Theory]
    [InlineData(0, "none")]
    [InlineData(1, "NoMasterSync")]
    [InlineData(2, "WeatherStale")]
    [InlineData(4, "InvalidWrite")]
    [InlineData(5, "NoMasterSync, InvalidWrite")]
    [InlineData(7, "NoMasterSync, WeatherStale, InvalidWrite")]
    public void WarnText_Flags_NamesSetBits(int flags, string expected)
    {
        // Arrange
        HeaderSnapshot header = HeaderSnapshot.decode(createRaw((ushort)flags, 0));

        // Act
        string text = header.warnText();

        // Assert
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(-500)]
    public void Decode_PowerSign_IsKept(int powerW)
    {
        // Act
        HeaderSnapshot header = HeaderSnapshot.decode(createRaw(0, powerW));

        // Assert
        Assert.Equal(powerW, header.ActivePowerW);
    }

    [Fact]
    public void HasInvalidWrite_BitTwoSet_ReturnsTrue()
    {
        // Act
        HeaderSnapshot header = HeaderSnapshot.decode(createRaw(RegisterMap.WARN_INVALID_WRITE, 0));

        // Assert
        Assert.True(header.hasInvalidWrite());
    }

    [Fact]
    public void Decode_UnknownDeviceType_Throws()
    {
        // Arrange
        ushort[] raw = createRaw(0, 0);
        raw[RegisterMap.IN_DEVICE_TYPE] = 9;

        // Act
        Action decode = delegate { HeaderSnapshot.decode(raw); };

        // Assert
        Assert.Throws<DiagnosticException>(decode);
    }

    [Fact]
    public void Decode_UnknownMapVersion_Throws()
    {
        // Arrange
        ushort[] raw = createRaw(0, 0);
        raw[RegisterMap.IN_MAP_VERSION] = 2;

        // Act
        Action decode = delegate { HeaderSnapshot.decode(raw); };

        // Assert
        Assert.Throws<DiagnosticException>(decode);
    }

    [Fact]
    public void Decode_TooFewRegisters_Throws()
    {
        // Act
        Action decode = delegate { HeaderSnapshot.decode(new ushort[5]); };

        // Assert
        Assert.Throws<DiagnosticException>(decode);
    }
}
