using EasyModbus;

namespace EnergySim.Modbus;

/// <summary>Synchronous EasyModbus calls isolated at the library boundary.</summary>
public sealed class ClockModbusClient : IDisposable
{
    private readonly ModbusClient _client;

    public ClockModbusClient(string host, int port, byte unitIdentifier)
    {
        _client = new ModbusClient(host, port);
        _client.UnitIdentifier = unitIdentifier;
        _client.ConnectionTimeout = 500;
        _client.NumberOfRetries = 0;
    }

    public void connect()
    {
        _client.Connect();
    }

    public int[] readInputRegisters(int startingAddress, int count)
    {
        return _client.ReadInputRegisters(startingAddress, count);
    }

    public int[] readHoldingRegisters(int startingAddress, int count)
    {
        return _client.ReadHoldingRegisters(startingAddress, count);
    }

    public void writeSingleRegister(int address, int value)
    {
        _client.WriteSingleRegister(address, value);
    }

    public void writeMultipleRegisters(int address, int[] values)
    {
        _client.WriteMultipleRegisters(address, values);
    }

    public void Disconnect()
    {
        if (_client.Connected)
        {
            _client.Disconnect();
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}
