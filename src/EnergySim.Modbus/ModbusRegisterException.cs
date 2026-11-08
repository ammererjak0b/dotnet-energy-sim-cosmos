namespace EnergySim.Modbus;

/// <summary>A register call failed. Carries what the caller needs for a readable message, hides the library exception type.</summary>
public sealed class ModbusRegisterException : Exception
{
    public ModbusRegisterException(string message, bool isProtocolError, bool wasConnected, Exception inner)
        : base(message, inner)
    {
        IsProtocolError = isProtocolError; // device answered with a Modbus error
        WasConnected = wasConnected; // false = connect itself failed
    }

    public bool IsProtocolError { get; }

    public bool WasConnected { get; }
}
