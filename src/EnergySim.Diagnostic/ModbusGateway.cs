using EasyModbus;
using EnergySim.Modbus;

namespace EnergySim.Diagnostic;

/// <summary>
/// Only place that calls the synchronous EasyModbus client. Connects on demand, drops the connection on any failure
/// (next command reconnects) and turns library errors into readable DiagnosticExceptions.
/// </summary>
public sealed class ModbusGateway : IDisposable
{
    private const int CONNECTION_TIMEOUT_MS = 2000;

    private readonly DiagnosticTarget _target;
    private readonly byte _unitId;
    private ModbusClient? _client;

    public ModbusGateway(DiagnosticTarget target, byte unitId)
    {
        _target = target;
        _unitId = unitId;
    }

    public DiagnosticTarget Target
    {
        get { return _target; }
    }

    public Task<ushort[]> readInputRegistersAsync(int startingAddress, int count, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ushort[] registers;
        try
        {
            ModbusClient client = connectIfNeeded();
            registers = toRegisters(client.ReadInputRegisters(startingAddress, count));
        }
        catch (Exception exception)
        {
            throw failed(exception, "read input registers " + startingAddress + ".." + (startingAddress + count - 1));
        }

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(registers);
    }

    public Task<ushort[]> readHoldingRegistersAsync(int startingAddress, int count, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ushort[] registers;
        try
        {
            ModbusClient client = connectIfNeeded();
            registers = toRegisters(client.ReadHoldingRegisters(startingAddress, count));
        }
        catch (Exception exception)
        {
            throw failed(exception, "read holding registers " + startingAddress + ".." + (startingAddress + count - 1));
        }

        ct.ThrowIfCancellationRequested();
        return Task.FromResult(registers);
    }

    public Task writeSingleRegisterAsync(int address, int value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            ModbusClient client = connectIfNeeded();
            client.WriteSingleRegister(address, value); // FC06
        }
        catch (Exception exception)
        {
            throw failed(exception, "write holding register " + address);
        }

        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void disconnect()
    {
        if (_client == null)
        {
            return;
        }

        try
        {
            _client.Disconnect();
        }
        catch (Exception)
        {
            // already broken, nothing to close
        }

        _client = null;
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
        ModbusClient client = new ModbusClient(_target.Host, _target.Port);
        client.UnitIdentifier = _unitId;
        client.ConnectionTimeout = CONNECTION_TIMEOUT_MS;
        client.NumberOfRetries = 0;
        client.Connect();
        _client = client;
        return client;
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

    private DiagnosticException failed(Exception exception, string action)
    {
        bool wasConnected = _client != null && _client.Connected;
        disconnect(); // reconnect on next command
        string endpoint = _target.Host + ":" + _target.Port;
        if (exception is EasyModbus.Exceptions.ModbusException)
        {
            return new DiagnosticException("protocol error on " + endpoint + " (" + action + "): " + exception.Message, false);
        }

        if (wasConnected)
        {
            return new DiagnosticException("lost connection to " + endpoint + " (" + action + "): " + exception.Message + ". " + _target.Name + " probably stopped, next command reconnects", false);
        }

        return new DiagnosticException("cannot reach " + endpoint + " (" + exception.Message + "). " + _target.Name + " is probably not running", false);
    }
}
