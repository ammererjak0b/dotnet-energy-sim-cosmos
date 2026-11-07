using System.Threading.Channels;
using EasyModbus;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Modbus;
using EnergySim.Tests.Support;

namespace EnergySim.Tests.Modbus;

/// <summary>Real DeviceModbusServer on a free port plus a real EasyModbus client. Dispose stops both.</summary>
internal sealed class DeviceServerHarness : IDisposable
{
    private const int CLIENT_TIMEOUT_MILLISECONDS = 1000;
    private const int COMMAND_WAIT_MILLISECONDS = 3000;
    private const int START_ATTEMPTS = 20;

    private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
    private readonly Channel<DeviceCommand> _target = Channel.CreateUnbounded<DeviceCommand>();
    private readonly DeviceType _type;
    private readonly byte _unitId;
    private ModbusClient? _client;
    private Task? _forwardTask;

    public DeviceServerHarness(DeviceType type, byte unitId)
    {
        _type = type;
        _unitId = unitId;
        Options = createOptions();
        Server = new DeviceModbusServer(Options);
        Sink = new DeviceModbusSink(Server);
    }

    public DeviceModbusOptions Options { get; private set; }

    public DeviceModbusServer Server { get; private set; }

    public DeviceModbusSink Sink { get; private set; }

    public ChannelReader<DeviceCommand> ForwardedCommands
    {
        get
        {
            return _target.Reader;
        }
    }

    /// <summary>Starts the server, tries the next port when one is taken, connects the client.</summary>
    public async Task startAsync()
    {
        for (int attempt = 0; attempt < START_ATTEMPTS; attempt++)
        {
            try
            {
                await Server.startAsync(CancellationToken.None);
                break;
            }
            catch (DeviceModbusPortInUseException)
            {
                Server.Dispose();
                Options = createOptions();
                Server = new DeviceModbusServer(Options);
                Sink = new DeviceModbusSink(Server);
            }
        }

        _forwardTask = Server.runCommandsAsync(_target.Writer, _shutdown.Token);
        _client = createClient(_unitId);
    }

    public ModbusClient createClient(byte unitId)
    {
        ModbusClient client = new ModbusClient("127.0.0.1", Options.Port);
        client.UnitIdentifier = unitId;
        client.ConnectionTimeout = CLIENT_TIMEOUT_MILLISECONDS;
        client.NumberOfRetries = 0;
        client.Connect();
        return client;
    }

    public async Task publishAsync(SimulationFrame frame)
    {
        await Sink.publishAsync(frame, CancellationToken.None);
    }

    public ushort[] readInput(int address, int count)
    {
        return toRegisters(_client!.ReadInputRegisters(address, count));
    }

    public ushort[] readHolding(int address, int count)
    {
        return toRegisters(_client!.ReadHoldingRegisters(address, count));
    }

    public void write(int address, int value)
    {
        _client!.WriteSingleRegister(address, value);
    }

    public async Task<DeviceCommand?> nextCommandAsync()
    {
        using CancellationTokenSource timeout = new CancellationTokenSource(COMMAND_WAIT_MILLISECONDS);
        try
        {
            return await _target.Reader.ReadAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task waitUntilHoldingAsync(int address, ushort expected)
    {
        await TestWait.untilAsync(delegate { return readHolding(address, 1)[0] == expected; });
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        if (_client != null)
        {
            _client.Disconnect();
        }

        Server.Dispose();
        if (_forwardTask != null)
        {
            _forwardTask.GetAwaiter().GetResult();
        }

        _shutdown.Dispose();
    }

    private DeviceModbusOptions createOptions()
    {
        DeviceModbusOptions options = new DeviceModbusOptions();
        options.Host = "127.0.0.1";
        options.Port = TestPorts.next(TestPorts.MODBUS_FIRST, TestPorts.MODBUS_LAST);
        options.UnitId = _unitId;
        options.DeviceId = "test-device";
        options.DeviceType = _type;
        return options;
    }

    private static ushort[] toRegisters(int[] raw)
    {
        ushort[] registers = new ushort[raw.Length];
        for (int index = 0; index < raw.Length; index++)
        {
            registers[index] = RegisterCodec.toRegister(raw[index]);
        }

        return registers;
    }
}
