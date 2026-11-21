using EnergySim.Core.Devices;
using EnergySim.Modbus;

namespace EnergySim.IntegrationTests;

/// <summary>
/// Real communication: a started device Modbus server (real TCP socket on a free local port) and the real register client.
/// The device is a real PV model with a fixed weather sample, the expected values follow from the PV formula
/// (800 W/m2, 20 degC ambient: cell 40 degC, derating 0.94, 5 kWp x 0.8 x 0.94 = 3.008 kW) and from docs/contracts/modbus-registers.md.
/// Needs no external service, only a free local port. Every test starts and stops its own server.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DeviceModbusServerTests : IAsyncLifetime
{
    private const int WAIT_TIMEOUT_SECONDS = 5; // client write must show up in time
    private const int POLL_MILLISECONDS = 20;
    private const uint SEQUENCE = 7;

    private PvServerHarness? _harness;
    private ModbusRegisterClient? _client;

    /// <summary>Arrange for all tests: free port, server started, client created.</summary>
    public async Task InitializeAsync()
    {
        _harness = await PvServerHarness.startAsync();
        _client = new ModbusRegisterClient(PvServerHarness.HOST, _harness.Port, PvServerHarness.UNIT_ID);
    }

    /// <summary>Runs also when a test failed: client and server are always released.</summary>
    public Task DisposeAsync()
    {
        if (_client != null)
        {
            _client.Dispose();
        }

        if (_harness != null)
        {
            _harness.Dispose();
        }

        return Task.CompletedTask;
    }

    [Fact]
    public void Read_BeforeFirstFrame_ShowsTypeAndMapVersionAndNoMasterSync()
    {
        // Arrange
        ModbusRegisterClient client = getClient();

        // Act
        HeaderSnapshot header = HeaderSnapshot.decode(client.readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT));

        // Assert
        Assert.Equal(DeviceType.Pv, header.Type);
        Assert.Equal(RegisterMap.MAP_VERSION, header.Raw[RegisterMap.IN_MAP_VERSION]);
        Assert.Equal(ClockStateCode.Unsynced, header.ClockState);
        Assert.True((header.WarnFlags & RegisterMap.WARN_NO_MASTER_SYNC) != 0);
    }

    [Fact]
    public void Read_PublishedPvMidday_ReturnsDecodedHeaderAndDeviceBlock()
    {
        // Arrange
        getHarness().publishMiddayFrame(SEQUENCE);
        ModbusRegisterClient client = getClient();

        // Act
        HeaderSnapshot header = HeaderSnapshot.decode(client.readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT));
        ushort[] deviceBlock = client.readInputRegisters(RegisterMap.SPECIFIC_START, 4 * RegisterMap.SPECIFIC_REGISTERS_PER_VALUE);

        // Assert header: power 3.008 kW, running, time and sequence as published
        Assert.Equal(DeviceType.Pv, header.Type);
        Assert.Equal(DeviceState.Running, header.State);
        Assert.Equal(PvServerHarness.POWER_W, header.ActivePowerW);
        Assert.Equal(0, header.FaultCode);
        Assert.Equal(PvServerHarness.VirtualTime, header.VirtualTime);
        Assert.Equal(SEQUENCE, header.Sequence);
        Assert.Equal(ClockStateCode.Running, header.ClockState);
        Assert.Equal(0, header.WarnFlags);

        // Assert device block: Irradiance x1, ModuleTemperature x10, SunElevation x10 (profile Pv)
        Assert.Equal(800, RegisterCodec.readInt32(deviceBlock, 0));
        Assert.Equal(400, RegisterCodec.readInt32(deviceBlock, 2));
        Assert.Equal(600, RegisterCodec.readInt32(deviceBlock, 4));
    }

    [Fact]
    public async Task Write_ValidLimit_QueuesSetLimitCommandInPercent()
    {
        // Arrange
        getHarness().publishMiddayFrame(SEQUENCE);
        ModbusRegisterClient client = getClient();

        // Act
        client.writeSingleRegister(RegisterMap.HOLD_LIMIT_PERMILLE, 500); // 50.0 %
        DeviceCommand? command = await waitForCommandAsync();

        // Assert
        Assert.NotNull(command);
        Assert.Equal(CommonCommands.SET_LIMIT, command.Name);
        Assert.Equal(50.0, command.Value, 6);
        HeaderSnapshot header = HeaderSnapshot.decode(client.readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT));
        Assert.False(header.hasInvalidWrite());
    }

    [Fact]
    public async Task Write_LimitAboveRange_QueuesNothingAndSetsInvalidWriteFlagUntilNextValidWrite()
    {
        // Arrange
        getHarness().publishMiddayFrame(SEQUENCE);
        ModbusRegisterClient client = getClient();

        // Act
        client.writeSingleRegister(RegisterMap.HOLD_LIMIT_PERMILLE, 1500); // above 100.0 %
        HeaderSnapshot afterInvalid = await waitForHeaderAsync(delegate (HeaderSnapshot snapshot) { return snapshot.hasInvalidWrite(); });
        client.writeSingleRegister(RegisterMap.HOLD_LIMIT_PERMILLE, 300);
        HeaderSnapshot afterValid = await waitForHeaderAsync(delegate (HeaderSnapshot snapshot) { return !snapshot.hasInvalidWrite(); });
        DeviceCommand? command = await waitForCommandAsync();

        // Assert: only the valid write became a command, the flag followed the writes
        Assert.True(afterInvalid.hasInvalidWrite());
        Assert.False(afterValid.hasInvalidWrite());
        Assert.NotNull(command);
        Assert.Equal(30.0, command.Value, 6);
        Assert.False(getHarness().Commands.Reader.TryRead(out DeviceCommand? extra), "the rejected write must not queue a command");
        Assert.Null(extra);
    }

    private ModbusRegisterClient getClient()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("client not created");
        }

        return _client;
    }

    private PvServerHarness getHarness()
    {
        if (_harness == null)
        {
            throw new InvalidOperationException("server not started");
        }

        return _harness;
    }

    /// <summary>The library handles a write on its own thread, so the effect shows up a moment after the write returns.</summary>
    private async Task<DeviceCommand?> waitForCommandAsync()
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(WAIT_TIMEOUT_SECONDS);
        while (DateTime.UtcNow < deadline)
        {
            if (getHarness().Commands.Reader.TryRead(out DeviceCommand? command))
            {
                return command;
            }

            await Task.Delay(POLL_MILLISECONDS);
        }

        return null;
    }

    private async Task<HeaderSnapshot> waitForHeaderAsync(Func<HeaderSnapshot, bool> isExpected)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(WAIT_TIMEOUT_SECONDS);
        HeaderSnapshot header = HeaderSnapshot.decode(getClient().readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT));
        while (!isExpected(header) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(POLL_MILLISECONDS);
            header = HeaderSnapshot.decode(getClient().readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT));
        }

        return header;
    }
}
