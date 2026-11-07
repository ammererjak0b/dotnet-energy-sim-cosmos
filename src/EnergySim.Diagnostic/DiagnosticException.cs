namespace EnergySim.Diagnostic;

/// <summary>Readable failure of one command (connection, protocol, bad register content). Message is shown as is.</summary>
public sealed class DiagnosticException : Exception
{
    public DiagnosticException(string message, bool isFatal)
        : base(message)
    {
        IsFatal = isFatal; // fatal = stop the program (unknown device type or map version)
    }

    public bool IsFatal { get; }
}
