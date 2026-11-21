namespace EnergySim.Modbus;

/// <summary>Header registers cannot be decoded (wrong length, unknown device type, map version or clock state).</summary>
public sealed class HeaderDecodeException : Exception
{
    public HeaderDecodeException(string message, bool isFatal)
        : base(message)
    {
        IsFatal = isFatal; // fatal = unknown type, map version or clock state, client cannot continue
    }

    public bool IsFatal { get; }
}
