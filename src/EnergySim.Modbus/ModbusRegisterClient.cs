using EasyModbus;

namespace EnergySim.Modbus;

/// <summary>
/// Register client for one Modbus TCP device. EasyModbus is synchronous, so the methods are synchronous too
/// (library boundary, no Async suffix). Connects on demand, drops the connection on any failure (next call reconnects).
/// </summary>
public sealed class ModbusRegisterClient : IDisposable
{
    private const int CONNECTION_TIMEOUT_MILLISECONDS = 2000;

    private readonly string _host;
    private readonly int _port;
    private readonly byte _unitIdentifier;
    private ModbusClient? _client;

    public ModbusRegisterClient(string host, int port, byte unitIdentifier)
    {
        _host = host;
        _port = port;
        _unitIdentifier = unitIdentifier;
    }

    /// <summary>Reads input registers (FC04). Throws ModbusRegisterException.</summary>
    public ushort[] readInputRegisters(int startingAddress, int count)
    {
        try
        {
            int[] raw = connectIfNeeded().ReadInputRegisters(startingAddress, count);
            return toRegisters(raw);
        }
        catch (Exception exception)
        {
            throw failed(exception);
        }
    }

    /// <summary>Writes one holding register (FC06). Throws ModbusRegisterException.</summary>
    public void writeSingleRegister(int address, int value)
    {
        try
        {
            connectIfNeeded().WriteSingleRegister(address, value);
        }
        catch (Exception exception)
        {
            throw failed(exception);
        }
    }

    public void Dispose()
    {
        disconnect();
    }

    private ModbusClient connectIfNeeded()
    {
        if (_client != null && _client.Connected)
        {
            return _client;
        }

        disconnect();
        ModbusClient client = new ModbusClient(_host, _port);
        client.UnitIdentifier = _unitIdentifier;
        client.ConnectionTimeout = CONNECTION_TIMEOUT_MILLISECONDS;
        client.NumberOfRetries = 0;
        client.Connect();
        _client = client;
        return client;
    }

    private void disconnect()
    {
        ModbusClient? client = _client;
        _client = null;
        if (client == null)
        {
            return;
        }

        client.Disconnect();
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

    private ModbusRegisterException failed(Exception exception)
    {
        bool wasConnected = _client != null && _client.Connected;
        disconnect(); // reconnect on next call
        bool isProtocolError = exception is EasyModbus.Exceptions.ModbusException;
        return new ModbusRegisterException(exception.Message, isProtocolError, wasConnected, exception);
    }
}
