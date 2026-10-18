namespace EnergySim.Core.Devices;

public sealed class CommandResult
{
    public CommandResult(bool accepted, string message)
    {
        Accepted = accepted;
        Message = message;
    }

    public bool Accepted { get; }

    public string Message { get; }
}
