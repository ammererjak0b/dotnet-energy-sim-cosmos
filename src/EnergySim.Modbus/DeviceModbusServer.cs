using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using EasyModbus;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;

namespace EnergySim.Modbus;

/// <summary>
/// Per-device Modbus TCP server. EasyModbus is synchronous: Listen/StopListening and its event run on library threads.
/// The +1 offset of the 1-based server arrays exists only in this class.
/// </summary>
public sealed class DeviceModbusServer : IDisposable
{
    private const int SERVER_OFFSET = 1; // library arrays are 1-based
    private const int ON_OFF_MAX = 1;
    private const int FAULT_RESET_REQUEST = 1;
    private const int FAULT_RESET_IDLE = 0;
    private const int LISTEN_WAIT_ATTEMPTS = 100;
    private const int LISTEN_WAIT_MILLISECONDS = 20;
    private const double PERMILLE_PER_PERCENT = 10.0;

    private readonly DeviceModbusOptions _options;
    private readonly ModbusServer _server;
    private readonly IReadOnlyList<DeviceProfileValue> _profile;
    private readonly Channel<DeviceCommand> _commands = Channel.CreateUnbounded<DeviceCommand>();
    private readonly object _lock = new object(); // guards our register writes, not the library reads
    private readonly ushort[] _lastHolding = new ushort[RegisterMap.HOLD_REGISTER_COUNT]; // last valid values
    private bool _started;
    private bool _holdingInitialized;
    private ushort _lastFrameOnOff; // device values of the previous frame, mirror only on change
    private ushort _lastFrameLimit;
    private bool _invalidWrite;
    private ushort _frameWarnFlags;

    public DeviceModbusServer(DeviceModbusOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.validate();
        _options = options;
        _profile = DeviceProfiles.forType(options.DeviceType);
        _server = new ModbusServer();
        _server.LocalIPAddress = IPAddress.Parse(options.Host);
        _server.Port = options.Port;
        _server.UnitIdentifier = options.UnitId;
        _server.HoldingRegistersChanged += onHoldingRegistersChanged;
        writeInputHeaderStatics();
    }

    /// <summary>Valid writes as device commands, already translated. Use runCommandsAsync to forward them.</summary>
    public ChannelReader<DeviceCommand> Commands
    {
        get
        {
            return _commands.Reader;
        }
    }

    /// <summary>Opens the port. Throws DeviceModbusPortInUseException if taken. Synchronous library call.</summary>
    public async Task startAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_started)
        {
            return;
        }

        checkPortIsFree(); // Listen() never throws, a bind error would kill the process on the library thread
        _server.Listen();
        _started = true;
        await waitUntilListeningAsync(ct); // Listen() returns before the library thread has bound the port
    }

    private async Task waitUntilListeningAsync(CancellationToken ct)
    {
        IPAddress probeAddress = IPAddress.Parse(_options.Host);
        if (probeAddress.Equals(IPAddress.Any))
        {
            probeAddress = IPAddress.Loopback;
        }

        for (int attempt = 0; attempt < LISTEN_WAIT_ATTEMPTS; attempt++)
        {
            using (TcpClient probe = new TcpClient())
            {
                try
                {
                    await probe.ConnectAsync(probeAddress, _options.Port, ct);
                    return;
                }
                catch (SocketException)
                {
                    // not bound yet
                }
            }

            await Task.Delay(LISTEN_WAIT_MILLISECONDS, ct);
        }

        throw new InvalidOperationException("Modbus server did not start listening on port " + _options.Port + ".");
    }

    /// <summary>Closes the port, completes the command channel. Synchronous library call.</summary>
    public Task stopAsync(CancellationToken ct)
    {
        if (_started)
        {
            _server.StopListening();
            _started = false;
        }

        _commands.Writer.TryComplete();
        return Task.CompletedTask;
    }

    /// <summary>Moves valid register writes into target (the loop's command channel) until ct is cancelled or the server stops.</summary>
    public async Task runCommandsAsync(ChannelWriter<DeviceCommand> target, CancellationToken ct)
    {
        try
        {
            await foreach (DeviceCommand command in _commands.Reader.ReadAllAsync(ct))
            {
                await target.WriteAsync(command, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // normal shutdown
        }
    }

    public void Dispose()
    {
        _server.HoldingRegistersChanged -= onHoldingRegistersChanged;
        if (_started)
        {
            _server.StopListening();
            _started = false;
        }

        _commands.Writer.TryComplete();
    }

    /// <summary>Maps one frame to the input registers (and the holding registers on the first frame and on device-side changes).</summary>
    public void updateFrame(SimulationFrame frame)
    {
        if (frame == null)
        {
            throw new ArgumentNullException(nameof(frame));
        }

        ushort[] block = buildInputBlock(frame);
        lock (_lock)
        {
            _frameWarnFlags = calculateFrameWarnFlags(frame);
            copyToServer(_server.inputRegisters.localArray, 0, block);
            writeWarnRegister();
            if (!_holdingInitialized)
            {
                _holdingInitialized = true;
                initHolding(frame.Header);
            }
            else
            {
                mirrorHoldingChanges(frame.Header);
            }
        }
    }

    /// <summary>Probe bind. Small race until Listen() binds, accepted.</summary>
    private void checkPortIsFree()
    {
        TcpListener probe = new TcpListener(IPAddress.Parse(_options.Host), _options.Port);
        try
        {
            probe.Start();
        }
        catch (SocketException exception)
        {
            throw new DeviceModbusPortInUseException(_options.Host, _options.Port, exception);
        }
        finally
        {
            probe.Stop();
        }
    }

    private void writeInputHeaderStatics()
    {
        lock (_lock)
        {
            _server.inputRegisters.localArray[RegisterMap.IN_DEVICE_TYPE + SERVER_OFFSET] = (short)(int)_options.DeviceType;
            _server.inputRegisters.localArray[RegisterMap.IN_MAP_VERSION + SERVER_OFFSET] = (short)RegisterMap.MAP_VERSION;
            _server.inputRegisters.localArray[RegisterMap.IN_CLOCK_STATE + SERVER_OFFSET] = (short)(int)ClockStateCode.Unsynced;
            _frameWarnFlags = RegisterMap.WARN_NO_MASTER_SYNC;
            writeWarnRegister();
        }
    }

    private ushort[] buildInputBlock(SimulationFrame frame)
    {
        int valueCount = Math.Min(_profile.Count, frame.SpecificValues.Count);
        int blockLength = RegisterMap.SPECIFIC_START + RegisterMap.SPECIFIC_REGISTERS_PER_VALUE * valueCount;
        ushort[] block = new ushort[blockLength]; // addresses 0.. client side
        DeviceHeader header = frame.Header;
        block[RegisterMap.IN_DEVICE_TYPE] = (ushort)(int)_options.DeviceType;
        block[RegisterMap.IN_MAP_VERSION] = (ushort)RegisterMap.MAP_VERSION;
        block[RegisterMap.IN_STATE] = (ushort)(int)header.State;
        block[RegisterMap.IN_FAULT_CODE] = header.FaultCode;
        RegisterCodec.writeInt32(block, RegisterMap.IN_ACTIVE_POWER_W, header.ActivePowerW);
        RegisterCodec.writeUInt32(block, RegisterMap.IN_ENERGY_WH, clampToUInt32(header.EnergyWh));
        RegisterCodec.writeUInt64(block, RegisterMap.IN_VIRTUAL_TIME_MS, (ulong)Math.Max(0L, frame.VirtualNow.ToUnixTimeMilliseconds()));
        RegisterCodec.writeUInt32(block, RegisterMap.IN_SEQUENCE, frame.Sequence);
        block[RegisterMap.IN_GENERATION] = (ushort)frame.Snapshot.Generation; // wraps at 65536
        block[RegisterMap.IN_CLOCK_STATE] = (ushort)(int)calculateClockState(frame.Snapshot);
        for (int index = 0; index < valueCount; index++)
        {
            int raw = RegisterCodec.scaleToRaw(frame.SpecificValues[index].Value, _profile[index].Scale);
            RegisterCodec.writeInt32(block, RegisterMap.SPECIFIC_START + RegisterMap.SPECIFIC_REGISTERS_PER_VALUE * index, raw);
        }

        return block;
    }

    private static uint clampToUInt32(long value)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value > uint.MaxValue)
        {
            return uint.MaxValue;
        }

        return (uint)value;
    }

    private static ClockStateCode calculateClockState(ClockSnapshot snapshot)
    {
        if (!snapshot.IsSynced && !snapshot.IsUsingFallback)
        {
            return ClockStateCode.Unsynced;
        }

        if (snapshot.Status == ClockStatus.Running)
        {
            return ClockStateCode.Running;
        }

        if (snapshot.Status == ClockStatus.Paused)
        {
            return ClockStateCode.Paused;
        }

        return ClockStateCode.Stopped;
    }

    private static ushort calculateFrameWarnFlags(SimulationFrame frame)
    {
        ushort flags = 0;
        if (!frame.Snapshot.IsSynced || frame.Snapshot.IsUsingFallback)
        {
            flags = (ushort)(flags | RegisterMap.WARN_NO_MASTER_SYNC);
        }

        if (frame.WeatherStale)
        {
            flags = (ushort)(flags | RegisterMap.WARN_WEATHER_STALE);
        }

        return flags;
    }

    /// <summary>Caller holds _lock.</summary>
    private void writeWarnRegister()
    {
        ushort flags = _frameWarnFlags;
        if (_invalidWrite)
        {
            flags = (ushort)(flags | RegisterMap.WARN_INVALID_WRITE);
        }

        _server.inputRegisters.localArray[RegisterMap.IN_WARN_FLAGS + SERVER_OFFSET] = RegisterCodec.toServerShort(flags);
    }

    /// <summary>Caller holds _lock. Mirrors the device start state into the holding registers.</summary>
    private void initHolding(DeviceHeader header)
    {
        ushort onOff = 0;
        if (header.IsOn)
        {
            onOff = 1;
        }

        double permille = Math.Round(header.PowerLimitPercent * PERMILLE_PER_PERCENT);
        ushort limit = (ushort)Math.Clamp(permille, 0, RegisterMap.LIMIT_PERMILLE_MAX);
        setHolding(RegisterMap.HOLD_ON_OFF, onOff);
        setHolding(RegisterMap.HOLD_LIMIT_PERMILLE, limit);
        setHolding(RegisterMap.HOLD_FAULT_RESET, FAULT_RESET_IDLE);
        _lastFrameOnOff = onOff;
        _lastFrameLimit = limit;
    }

    /// <summary>
    /// Caller holds _lock. Device changed on/off or limit itself (keyboard, reset): copy to the holding registers.
    /// Only on change versus the previous frame, so a frame published before a queued client write is applied cannot overwrite it.
    /// </summary>
    private void mirrorHoldingChanges(DeviceHeader header)
    {
        ushort onOff = 0;
        if (header.IsOn)
        {
            onOff = 1;
        }

        double permille = Math.Round(header.PowerLimitPercent * PERMILLE_PER_PERCENT);
        ushort limit = (ushort)Math.Clamp(permille, 0, RegisterMap.LIMIT_PERMILLE_MAX);
        if (onOff != _lastFrameOnOff)
        {
            setHolding(RegisterMap.HOLD_ON_OFF, onOff);
            _lastFrameOnOff = onOff;
        }

        if (limit != _lastFrameLimit)
        {
            setHolding(RegisterMap.HOLD_LIMIT_PERMILLE, limit);
            _lastFrameLimit = limit;
        }
    }

    /// <summary>Caller holds _lock.</summary>
    private void setHolding(int address, ushort value)
    {
        _lastHolding[address] = value;
        _server.holdingRegisters.localArray[address + SERVER_OFFSET] = RegisterCodec.toServerShort(value);
    }

    private static void copyToServer(short[] target, int address, ushort[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            target[address + index + SERVER_OFFSET] = RegisterCodec.toServerShort(values[index]);
        }
    }

    /// <summary>Library thread. The register is already written by the library: validate, restore if invalid, queue the command.</summary>
    private void onHoldingRegistersChanged(int startRegister, int numberOfRegisters)
    {
        lock (_lock)
        {
            int firstAddress = startRegister - SERVER_OFFSET; // event start is 1-based (see README)
            for (int index = 0; index < numberOfRegisters; index++)
            {
                int address = firstAddress + index;
                if (address >= 0 && address < RegisterMap.HOLD_REGISTER_COUNT)
                {
                    handleWrite(address);
                }
            }

            writeWarnRegister();
        }
    }

    /// <summary>Caller holds _lock.</summary>
    private void handleWrite(int address)
    {
        ushort value = RegisterCodec.toRegister(_server.holdingRegisters.localArray[address + SERVER_OFFSET]);
        if (address == RegisterMap.HOLD_ON_OFF)
        {
            handleOnOff(value);
        }
        else if (address == RegisterMap.HOLD_LIMIT_PERMILLE)
        {
            handleLimit(value);
        }
        else if (address == RegisterMap.HOLD_FAULT_RESET)
        {
            handleFaultReset(value);
        }
    }

    private void handleOnOff(ushort value)
    {
        if (value > ON_OFF_MAX)
        {
            rejectWrite(RegisterMap.HOLD_ON_OFF);
            return;
        }

        acceptWrite(RegisterMap.HOLD_ON_OFF, value);
        string commandName = CommonCommands.TURN_OFF;
        if (value == 1)
        {
            commandName = CommonCommands.TURN_ON;
        }

        _commands.Writer.TryWrite(new DeviceCommand(commandName, 0));
    }

    private void handleLimit(ushort value)
    {
        if (value > RegisterMap.LIMIT_PERMILLE_MAX)
        {
            rejectWrite(RegisterMap.HOLD_LIMIT_PERMILLE);
            return;
        }

        acceptWrite(RegisterMap.HOLD_LIMIT_PERMILLE, value);
        _commands.Writer.TryWrite(new DeviceCommand(CommonCommands.SET_LIMIT, value / PERMILLE_PER_PERCENT));
    }

    private void handleFaultReset(ushort value)
    {
        if (value > FAULT_RESET_REQUEST)
        {
            rejectWrite(RegisterMap.HOLD_FAULT_RESET);
            return;
        }

        _invalidWrite = false;
        if (value == FAULT_RESET_REQUEST)
        {
            _commands.Writer.TryWrite(new DeviceCommand(CommonCommands.RESET_FAULT, 0));
        }

        setHolding(RegisterMap.HOLD_FAULT_RESET, FAULT_RESET_IDLE); // always back to 0
    }

    private void acceptWrite(int address, ushort value)
    {
        _invalidWrite = false; // cleared by the next valid write
        _lastHolding[address] = value;
    }

    private void rejectWrite(int address)
    {
        _invalidWrite = true;
        setHolding(address, _lastHolding[address]); // restore old value
    }
}
