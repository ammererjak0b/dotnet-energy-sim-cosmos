namespace EnergySim.Terminal;

/// <summary>Decides between the Spectre dashboard and plain headless output.</summary>
public static class TerminalMode
{
    public static bool isInteractive()
    {
        return isInteractive(Console.IsOutputRedirected, Console.IsInputRedirected);
    }

    public static bool isInteractive(bool outputRedirected, bool inputRedirected)
    {
        if (outputRedirected)
        {
            return false;
        }

        if (inputRedirected)
        {
            return false;
        }

        return true;
    }
}
