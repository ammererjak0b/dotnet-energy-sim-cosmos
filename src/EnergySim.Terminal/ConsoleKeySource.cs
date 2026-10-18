namespace EnergySim.Terminal;

/// <summary>Real keyboard via Console.KeyAvailable / ReadKey(intercept).</summary>
public sealed class ConsoleKeySource : IKeySource
{
    public bool tryReadKey(out char key)
    {
        key = ' ';
        if (!TerminalMode.isInteractive())
        {
            return false; // KeyAvailable throws when input is redirected
        }

        if (!Console.KeyAvailable)
        {
            return false;
        }

        key = Console.ReadKey(true).KeyChar; // intercept = no echo
        return true;
    }
}
