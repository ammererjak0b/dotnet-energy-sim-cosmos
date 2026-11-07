namespace EnergySim.Diagnostic;

/// <summary>Wrong command line or settings. Message is shown to the user, exit code 2.</summary>
public sealed class DiagnosticUsageException : Exception
{
    public DiagnosticUsageException(string message)
        : base(message)
    {
    }
}
