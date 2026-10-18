namespace EnergySim.Cli;

/// <summary>Wrong command line or settings. Message is shown to the user, exit code 2.</summary>
public sealed class CliUsageException : Exception
{
    public CliUsageException(string message)
        : base(message)
    {
    }
}
