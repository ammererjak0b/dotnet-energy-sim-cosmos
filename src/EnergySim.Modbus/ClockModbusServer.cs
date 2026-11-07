using System.Net;
using System.Threading.Channels;
using EasyModbus;

namespace EnergySim.Modbus;

/// <summary>Loopback-only EasyModbus server with a channel boundary for received writes.</summary>
public sealed class ClockModbusServer : IDisposable
{
    private readonly ModbusServer _server;
    private readonly Channel<ClockRegisterWrite> _writes = Channel.CreateUnbounded<ClockRegisterWrite>();
    private bool _started;

    public int Port { get; }
    public ChannelReader<ClockRegisterWrite> Writes
    {
        get
        {
            return _writes.Reader;
        }
    }

    public void setInputRegister(int address, int value)
    {
        _server.inputRegisters.localArray[address + 1] = checked((short)value);
    }

    public ClockModbusServer(ClockEndpointOptions options)
    {
        options.validate();
        Port = options.Port;
        _server = new ModbusServer();
        _server.LocalIPAddress = IPAddress.Loopback;
        _server.Port = options.Port;
        _server.UnitIdentifier = 1;
        _server.FunctionCode1Disabled = true;
        _server.FunctionCode2Disabled = true;
        _server.FunctionCode3Disabled = false;
        _server.FunctionCode4Disabled = false;
        _server.FunctionCode5Disabled = true;
        _server.FunctionCode6Disabled = false;
        _server.FunctionCode15Disabled = true;
        _server.FunctionCode16Disabled = false;
        _server.FunctionCode23Disabled = true;
        _server.HoldingRegistersChanged += captureWrite;
    }

    /// <summary>Starts EasyModbus's listener; throws DeviceModbusPortInUseException (a SocketException) when the port is in use.</summary>
    public void start()
    {
        if (_started)
        {
            return;
        }

        PortProbe.ensureFree(IPAddress.Loopback, Port);
        _server.Listen();
        _started = true;
    }

    public void Dispose()
    {
        _server.HoldingRegistersChanged -= captureWrite;
        if (_started)
        {
            _server.StopListening();
            _started = false;
        }

        _writes.Writer.TryComplete();
    }

    private void captureWrite(int register, int numberOfRegisters)
    {
        if (register < 0 || numberOfRegisters <= 0)
        {
            return;
        }

        List<int> values = new List<int>();
        for (int index = 0; index < numberOfRegisters; index++)
        {
            values.Add((ushort)_server.holdingRegisters.localArray[register + index]);
        }

        _writes.Writer.TryWrite(new ClockRegisterWrite(register - 1, values));
    }
}
