using EnergySim.Modbus;

namespace EnergySim.Diagnostic;

/// <summary>
/// One target plus its register client. Checks the cancellation token around each (synchronous) call
/// and turns register errors into readable DiagnosticExceptions.
/// </summary>
public sealed class DiagnosticDevice : IDisposable
{
    private readonly ModbusRegisterClient _client;

    public DiagnosticDevice(DiagnosticTarget target, byte unitId)
    {
        Target = target;
        _client = new ModbusRegisterClient(target.Host, target.Port, unitId);
    }

    public DiagnosticTarget Target { get; }

    public ushort[] readInputRegisters(int startingAddress, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ushort[] registers;
        try
        {
            registers = _client.readInputRegisters(startingAddress, count);
        }
        catch (ModbusRegisterException exception)
        {
            throw failed(exception, "read input registers " + startingAddress + ".." + (startingAddress + count - 1));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return registers;
    }

    public void writeSingleRegister(int address, int value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _client.writeSingleRegister(address, value); // FC06
        }
        catch (ModbusRegisterException exception)
        {
            throw failed(exception, "write holding register " + address);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private DiagnosticException failed(ModbusRegisterException exception, string action)
    {
        string endpoint = Target.Host + ":" + Target.Port;
        if (exception.IsProtocolError)
        {
            return new DiagnosticException("protocol error on " + endpoint + " (" + action + "): " + exception.Message, false);
        }

        if (exception.WasConnected)
        {
            return new DiagnosticException("lost connection to " + endpoint + " (" + action + "): " + exception.Message + ". " + Target.Name + " probably stopped, next command reconnects", false);
        }

        return new DiagnosticException("cannot reach " + endpoint + " (" + exception.Message + "). " + Target.Name + " is probably not running", false);
    }
}
