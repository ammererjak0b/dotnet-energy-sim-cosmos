using EnergySim.Core.Devices;
using EnergySim.Gateway;
using EnergySim.Messaging;
using EnergySim.Modbus;

namespace EnergySim.UnitTests.Gateway;

/// <summary>Registers to message. Expected values from docs/contracts/modbus-registers.md and messaging.md.</summary>
public sealed class TelemetryMapperTests
{
    private static readonly DateTimeOffset SIM_TIME = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime POLLED_AT = new DateTime(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc);

    private static ushort[] createHeader(DeviceType type, DeviceState state, ushort faultCode, ushort warnFlags, ClockStateCode clockState)
    {
        ushort[] raw = new ushort[RegisterMap.HEADER_REGISTER_COUNT];
        raw[RegisterMap.IN_DEVICE_TYPE] = (ushort)(int)type;
        raw[RegisterMap.IN_MAP_VERSION] = RegisterMap.MAP_VERSION;
        raw[RegisterMap.IN_STATE] = (ushort)(int)state;
        raw[RegisterMap.IN_FAULT_CODE] = faultCode;
        raw[RegisterMap.IN_WARN_FLAGS] = warnFlags;
        RegisterCodec.writeInt32(raw, RegisterMap.IN_ACTIVE_POWER_W, 3008);
        RegisterCodec.writeUInt32(raw, RegisterMap.IN_ENERGY_WH, 15400);
        RegisterCodec.writeUInt64(raw, RegisterMap.IN_VIRTUAL_TIME_MS, (ulong)SIM_TIME.ToUnixTimeMilliseconds());
        RegisterCodec.writeUInt32(raw, RegisterMap.IN_SEQUENCE, 42);
        raw[RegisterMap.IN_GENERATION] = 2;
        raw[RegisterMap.IN_CLOCK_STATE] = (ushort)(int)clockState;
        return raw;
    }

    /// <summary>PV device block: Irradiance x1, ModuleTemperature x10, SunElevation x10, DailyEnergy x1.</summary>
    private static ushort[] createPvBlock()
    {
        ushort[] block = new ushort[8];
        RegisterCodec.writeInt32(block, 0, 800);
        RegisterCodec.writeInt32(block, 2, 400);
        RegisterCodec.writeInt32(block, 4, 600);
        RegisterCodec.writeInt32(block, 6, 250);
        return block;
    }

    private static TelemetryMessage mapPv(ushort faultCode, ushort warnFlags, ClockStateCode clockState)
    {
        HeaderSnapshot header = HeaderSnapshot.decode(createHeader(DeviceType.Pv, DeviceState.Running, faultCode, warnFlags, clockState));
        return TelemetryMapper.map("pv-1", header, createPvBlock(), POLLED_AT);
    }

    [Fact]
    public void Map_RunningSyncedPv_ReturnsGoodMessageWithAllFields()
    {
        // Act
        TelemetryMessage message = mapPv(0, 0, ClockStateCode.Running);

        // Assert
        Assert.Equal("pv-1", message.DeviceId);
        Assert.Equal("Pv", message.DeviceType);
        Assert.Equal(SIM_TIME.UtcDateTime, message.SimTimeUtc);
        Assert.Equal(POLLED_AT, message.PolledAtUtc);
        Assert.Equal(42u, message.Sequence);
        Assert.Equal(2, message.Generation);
        Assert.Equal("Running", message.ClockState);
        Assert.Equal("Running", message.State);
        Assert.Equal(TelemetryQuality.Good, message.Quality);
        Assert.Empty(message.WarnFlags);
        Assert.Equal(string.Empty, message.Error);
    }

    [Fact]
    public void Map_Pv_MeasurementsHaveHeaderValuesThenProfileValuesWithUnitsAndScale()
    {
        // Act
        TelemetryMessage message = mapPv(0, 0, ClockStateCode.Running);

        // Assert
        List<string> names = new List<string>();
        foreach (Measurement measurement in message.Measurements)
        {
            names.Add(measurement.Name);
        }

        Assert.Equal(new List<string> { "ActivePower", "Energy", "Irradiance", "ModuleTemperature", "SunElevation", "DailyEnergy" }, names);
        Assert.Equal(3008, message.Measurements[0].Value);
        Assert.Equal("W", message.Measurements[0].Unit);
        Assert.Equal(15400, message.Measurements[1].Value);
        Assert.Equal("Wh", message.Measurements[1].Unit);
        Assert.Equal(800, message.Measurements[2].Value);
        Assert.Equal(40.0, message.Measurements[3].Value, 6); // raw 400 / scale 10
        Assert.Equal(60.0, message.Measurements[4].Value, 6);
        Assert.Equal(250, message.Measurements[5].Value);
    }

    [Theory]
    [InlineData(RegisterMap.WARN_NO_MASTER_SYNC, ClockStateCode.Running, "NoMasterSync")]
    [InlineData(RegisterMap.WARN_WEATHER_STALE, ClockStateCode.Running, "WeatherStale")]
    [InlineData(0, ClockStateCode.Unsynced, "")]
    public void Map_WarningOrUnsyncedClock_IsUncertain(ushort warnFlags, ClockStateCode clockState, string expectedWarning)
    {
        // Act
        TelemetryMessage message = mapPv(0, warnFlags, clockState);

        // Assert
        Assert.Equal(TelemetryQuality.Uncertain, message.Quality);
        if (expectedWarning.Length > 0)
        {
            Assert.Contains(expectedWarning, message.WarnFlags);
        }
    }

    [Fact]
    public void Map_InvalidWriteWarning_StaysGood()
    {
        // Act
        TelemetryMessage message = mapPv(0, RegisterMap.WARN_INVALID_WRITE, ClockStateCode.Running);

        // Assert: contract, only NoMasterSync, WeatherStale and an unsynced clock lower the quality
        Assert.Equal(TelemetryQuality.Good, message.Quality);
        Assert.Contains("InvalidWrite", message.WarnFlags);
    }

    [Fact]
    public void Map_DeviceFault_IsDataNotQuality()
    {
        // Arrange
        HeaderSnapshot header = HeaderSnapshot.decode(createHeader(DeviceType.Pv, DeviceState.Fault, 1, 0, ClockStateCode.Running));

        // Act
        TelemetryMessage message = TelemetryMapper.map("pv-1", header, createPvBlock(), POLLED_AT);

        // Assert
        Assert.Equal(TelemetryQuality.Good, message.Quality);
        Assert.Equal("Fault", message.State);
        Assert.Equal(1, message.FaultCode);
    }

    [Fact]
    public void Map_PausedClock_KeepsSimTimeAndIsGood()
    {
        // Act
        TelemetryMessage message = mapPv(0, 0, ClockStateCode.Paused);

        // Assert
        Assert.Equal("Paused", message.ClockState);
        Assert.Equal(TelemetryQuality.Good, message.Quality);
        Assert.Equal(SIM_TIME.UtcDateTime, message.SimTimeUtc);
    }

    [Fact]
    public void MapBad_FailedRead_HasNoValuesNoSimTimeAndTheReason()
    {
        // Arrange
        GatewayDeviceConfig device = new GatewayDeviceConfig("battery-1", DeviceType.Battery, "127.0.0.1", 1512);

        // Act
        TelemetryMessage message = TelemetryMapper.mapBad(device, POLLED_AT, "cannot reach 127.0.0.1:1512");

        // Assert
        Assert.Equal("battery-1", message.DeviceId);
        Assert.Equal("Battery", message.DeviceType);
        Assert.Equal(TelemetryQuality.Bad, message.Quality);
        Assert.Equal("cannot reach 127.0.0.1:1512", message.Error);
        Assert.Null(message.SimTimeUtc);
        Assert.Equal(POLLED_AT, message.PolledAtUtc);
        Assert.Empty(message.Measurements);
    }
}
