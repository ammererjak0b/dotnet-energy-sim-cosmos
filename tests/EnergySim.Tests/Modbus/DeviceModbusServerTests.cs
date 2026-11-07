using System.Threading.Channels;
using EasyModbus;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Tests.Modbus;

/// <summary>Real EasyModbus client against the real device server on a loopback port.</summary>
public sealed class DeviceModbusServerTests
{
    private const byte UNIT_ID = 1;

    private static async Task<DeviceServerHarness> startHarnessAsync(DeviceType type)
    {
        DeviceServerHarness harness = new DeviceServerHarness(type, UNIT_ID);
        await harness.startAsync();
        return harness;
    }

    [Fact]
    public async Task ReadHeader_AfterPublish_ShowsFrameValues()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);

        // Act
        await harness.publishAsync(ModbusFrames.createPv());
        ushort[] header = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);

        // Assert
        Assert.Equal((ushort)DeviceType.Pv, header[RegisterMap.IN_DEVICE_TYPE]);
        Assert.Equal(RegisterMap.MAP_VERSION, header[RegisterMap.IN_MAP_VERSION]);
        Assert.Equal((ushort)DeviceState.Running, header[RegisterMap.IN_STATE]);
        Assert.Equal(0, header[RegisterMap.IN_FAULT_CODE]);
        Assert.Equal(0, header[RegisterMap.IN_WARN_FLAGS]);
        Assert.Equal(-1500, RegisterCodec.readInt32(header, RegisterMap.IN_ACTIVE_POWER_W));
        Assert.Equal(123456u, RegisterCodec.readUInt32(header, RegisterMap.IN_ENERGY_WH));
        Assert.Equal((ulong)ModbusFrames.VIRTUAL_NOW.ToUnixTimeMilliseconds(), RegisterCodec.readUInt64(header, RegisterMap.IN_VIRTUAL_TIME_MS));
        Assert.Equal(70000u, RegisterCodec.readUInt32(header, RegisterMap.IN_SEQUENCE));
        Assert.Equal(3, header[RegisterMap.IN_GENERATION]);
        Assert.Equal((ushort)ClockStateCode.Running, header[RegisterMap.IN_CLOCK_STATE]);
    }

    [Fact]
    public async Task ReadHeader_GenerationPositivePower_KeepsSignPositive()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        frame.Header.ActivePowerW = 4200;

        // Act
        await harness.publishAsync(frame);
        ushort[] header = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);

        // Assert
        Assert.Equal(4200, RegisterCodec.readInt32(header, RegisterMap.IN_ACTIVE_POWER_W));
    }

    [Theory]
    [InlineData(ClockStatus.Running, true, ClockStateCode.Running, 0)]
    [InlineData(ClockStatus.Paused, true, ClockStateCode.Paused, 0)]
    [InlineData(ClockStatus.Stopped, true, ClockStateCode.Stopped, 0)]
    [InlineData(ClockStatus.Running, false, ClockStateCode.Unsynced, 1)]
    public async Task ReadHeader_ClockSnapshot_MapsClockStateAndSyncWarning(ClockStatus status, bool isSynced, ClockStateCode expectedState, int expectedWarn)
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        frame.Snapshot.Status = status;
        frame.Snapshot.IsSynced = isSynced;

        // Act
        await harness.publishAsync(frame);
        ushort[] header = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);

        // Assert
        Assert.Equal((ushort)expectedState, header[RegisterMap.IN_CLOCK_STATE]);
        Assert.Equal(expectedWarn, header[RegisterMap.IN_WARN_FLAGS]);
    }

    [Fact]
    public async Task ReadHeader_WeatherStale_SetsWarnBitOne()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        frame.WeatherStale = true;

        // Act
        await harness.publishAsync(frame);
        ushort[] header = harness.readInput(RegisterMap.IN_WARN_FLAGS, 1);

        // Assert
        Assert.Equal(RegisterMap.WARN_WEATHER_STALE, header[0]);
    }

    [Fact]
    public async Task ReadHeader_BeforeFirstFrame_ShowsTypeVersionAndUnsynced()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Wind);

        // Act
        ushort[] header = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);

        // Assert
        Assert.Equal((ushort)DeviceType.Wind, header[RegisterMap.IN_DEVICE_TYPE]);
        Assert.Equal(RegisterMap.MAP_VERSION, header[RegisterMap.IN_MAP_VERSION]);
        Assert.Equal((ushort)ClockStateCode.Unsynced, header[RegisterMap.IN_CLOCK_STATE]);
        Assert.Equal(RegisterMap.WARN_NO_MASTER_SYNC, header[RegisterMap.IN_WARN_FLAGS]);
    }

    [Theory]
    [InlineData(DeviceType.Pv)]
    [InlineData(DeviceType.Battery)]
    [InlineData(DeviceType.Charger)]
    [InlineData(DeviceType.Wind)]
    public async Task ReadDeviceBlock_PerDeviceType_ScaledByProfile(DeviceType type)
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(type);
        IReadOnlyList<DeviceProfileValue> profile = DeviceProfiles.forType(type);
        double[] values = new double[profile.Count];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = 12.34 + index * 100.55;
        }

        values[values.Length - 1] = -7.26; // negative value stays negative

        // Act
        await harness.publishAsync(ModbusFrames.create(type, values));
        ushort[] block = harness.readInput(RegisterMap.SPECIFIC_START, RegisterMap.SPECIFIC_REGISTERS_PER_VALUE * profile.Count);

        // Assert
        for (int index = 0; index < profile.Count; index++)
        {
            int raw = RegisterCodec.readInt32(block, RegisterMap.SPECIFIC_REGISTERS_PER_VALUE * index);
            Assert.Equal(RegisterCodec.scaleToRaw(values[index], profile[index].Scale), raw);
        }

        Assert.True(RegisterCodec.readInt32(block, RegisterMap.SPECIFIC_REGISTERS_PER_VALUE * (profile.Count - 1)) < 0);
    }

    [Fact]
    public async Task ReadInput_ClientAddressZeroAndHundred_MapToHeaderAndFirstDeviceValue()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());

        // Act
        ushort[] firstRegister = harness.readInput(0, 1);
        ushort[] stateRegister = harness.readInput(RegisterMap.IN_STATE, 1);
        ushort[] irradiance = harness.readInput(RegisterMap.SPECIFIC_START, RegisterMap.SPECIFIC_REGISTERS_PER_VALUE);

        // Assert
        Assert.Equal((ushort)DeviceType.Pv, firstRegister[0]);
        Assert.Equal((ushort)DeviceState.Running, stateRegister[0]);
        Assert.Equal(800, RegisterCodec.readInt32(irradiance, 0));
    }

    [Fact]
    public async Task ReadHolding_AfterFirstFrame_MirrorsDeviceOnOffAndLimit()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        frame.Header.PowerLimitPercent = 75.5;

        // Act
        await harness.publishAsync(frame);
        ushort[] holding = harness.readHolding(0, RegisterMap.HOLD_REGISTER_COUNT);

        // Assert
        Assert.Equal(1, holding[RegisterMap.HOLD_ON_OFF]);
        Assert.Equal(755, holding[RegisterMap.HOLD_LIMIT_PERMILLE]);
        Assert.Equal(0, holding[RegisterMap.HOLD_FAULT_RESET]);
    }

    [Theory]
    [InlineData(RegisterMap.HOLD_ON_OFF, 0, CommonCommands.TURN_OFF, 0.0)]
    [InlineData(RegisterMap.HOLD_ON_OFF, 1, CommonCommands.TURN_ON, 0.0)]
    [InlineData(RegisterMap.HOLD_LIMIT_PERMILLE, 500, CommonCommands.SET_LIMIT, 50.0)]
    [InlineData(RegisterMap.HOLD_LIMIT_PERMILLE, 1000, CommonCommands.SET_LIMIT, 100.0)]
    [InlineData(RegisterMap.HOLD_LIMIT_PERMILLE, 0, CommonCommands.SET_LIMIT, 0.0)]
    [InlineData(RegisterMap.HOLD_FAULT_RESET, 1, CommonCommands.RESET_FAULT, 0.0)]
    public async Task Write_ValidValue_ArrivesAsDeviceCommand(int address, int value, string expectedName, double expectedValue)
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());

        // Act
        harness.write(address, value);
        DeviceCommand? command = await harness.nextCommandAsync();

        // Assert
        Assert.NotNull(command);
        Assert.Equal(expectedName, command.Name);
        Assert.Equal(expectedValue, command.Value, 6);
    }

    [Fact]
    public async Task Write_ValidValue_ClearsEarlierInvalidWriteWarning()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());
        harness.write(RegisterMap.HOLD_LIMIT_PERMILLE, 1001);
        await harness.waitUntilHoldingAsync(RegisterMap.HOLD_LIMIT_PERMILLE, 1000);

        // Act
        harness.write(RegisterMap.HOLD_LIMIT_PERMILLE, 400);
        await harness.nextCommandAsync();
        ushort[] warn = harness.readInput(RegisterMap.IN_WARN_FLAGS, 1);

        // Assert
        Assert.Equal(0, warn[0] & RegisterMap.WARN_INVALID_WRITE);
    }

    [Theory]
    [InlineData(RegisterMap.HOLD_ON_OFF, 2, 1)]
    [InlineData(RegisterMap.HOLD_LIMIT_PERMILLE, 1001, 1000)]
    [InlineData(RegisterMap.HOLD_FAULT_RESET, 2, 0)]
    public async Task Write_InvalidValue_RestoresOldValueAndSetsWarnBitTwo(int address, int value, int expectedRestored)
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());

        // Act
        harness.write(address, value);
        await harness.waitUntilHoldingAsync(address, (ushort)expectedRestored);
        ushort[] holding = harness.readHolding(address, 1);
        ushort[] warn = harness.readInput(RegisterMap.IN_WARN_FLAGS, 1);

        // Assert
        Assert.Equal(expectedRestored, holding[0]);
        Assert.Equal(RegisterMap.WARN_INVALID_WRITE, warn[0] & RegisterMap.WARN_INVALID_WRITE);
        Assert.False(harness.ForwardedCommands.TryRead(out DeviceCommand? command));
    }

    [Fact]
    public async Task Write_FaultResetOne_RegisterReturnsToZero()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());

        // Act
        harness.write(RegisterMap.HOLD_FAULT_RESET, 1);
        await harness.nextCommandAsync();
        await harness.waitUntilHoldingAsync(RegisterMap.HOLD_FAULT_RESET, 0);
        ushort[] holding = harness.readHolding(RegisterMap.HOLD_FAULT_RESET, 1);

        // Assert
        Assert.Equal(0, holding[0]);
    }

    [Fact]
    public async Task UpdateFrame_SameFramePublishedAgain_KeepsRegistersAndSequence()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        await harness.publishAsync(frame);
        ushort[] before = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);
        ushort[] holdingBefore = harness.readHolding(0, RegisterMap.HOLD_REGISTER_COUNT);

        // Act
        await harness.publishAsync(frame);
        await harness.publishAsync(frame);
        ushort[] after = harness.readInput(0, RegisterMap.HEADER_REGISTER_COUNT);
        ushort[] holdingAfter = harness.readHolding(0, RegisterMap.HOLD_REGISTER_COUNT);

        // Assert
        Assert.Equal(before, after);
        Assert.Equal(70000u, RegisterCodec.readUInt32(after, RegisterMap.IN_SEQUENCE));
        Assert.Equal(holdingBefore, holdingAfter);
    }

    [Fact]
    public async Task UpdateFrame_DeviceChangesOnOffAndLimit_MirrorsIntoHoldingRegisters()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());
        SimulationFrame changed = ModbusFrames.createPv();
        changed.Header.IsOn = false;
        changed.Header.PowerLimitPercent = 40.0;

        // Act
        await harness.publishAsync(changed);
        ushort[] holding = harness.readHolding(0, RegisterMap.HOLD_REGISTER_COUNT);

        // Assert
        Assert.Equal(0, holding[RegisterMap.HOLD_ON_OFF]);
        Assert.Equal(400, holding[RegisterMap.HOLD_LIMIT_PERMILLE]);
    }

    [Fact]
    public async Task UpdateFrame_DeviceStateUnchanged_DoesNotOverwriteClientWrite()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        SimulationFrame frame = ModbusFrames.createPv();
        await harness.publishAsync(frame);
        harness.write(RegisterMap.HOLD_LIMIT_PERMILLE, 500);
        await harness.nextCommandAsync();

        // Act
        await harness.publishAsync(frame); // device did not apply the command yet
        ushort[] holding = harness.readHolding(RegisterMap.HOLD_LIMIT_PERMILLE, 1);

        // Assert
        Assert.Equal(500, holding[0]);
    }

    [Fact]
    public async Task Read_WrongUnitId_GetsNoAnswer()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        await harness.publishAsync(ModbusFrames.createPv());
        ModbusClient wrongClient = harness.createClient(2);
        wrongClient.ConnectionTimeout = 300;

        // Act
        Exception? exception = Record.Exception(delegate { wrongClient.ReadInputRegisters(0, 1); });
        wrongClient.Disconnect();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task StartAsync_PortInUse_ThrowsPortInUseException()
    {
        // Arrange
        using DeviceServerHarness harness = await startHarnessAsync(DeviceType.Pv);
        DeviceModbusOptions options = new DeviceModbusOptions();
        options.Host = "127.0.0.1";
        options.Port = harness.Options.Port;
        Channel<DeviceCommand> ownCommands = Channel.CreateUnbounded<DeviceCommand>();
        using DeviceModbusServer second = new DeviceModbusServer(options, ownCommands.Writer);

        // Act
        Func<Task> start = delegate { return second.startAsync(CancellationToken.None); };

        // Assert
        await Assert.ThrowsAsync<DeviceModbusPortInUseException>(start);
    }
}
