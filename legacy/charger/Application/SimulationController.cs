namespace LB_01.Application;

public class SimulationController
{
    private int nextStep = 1;

    public bool isRunning { get; private set; } = true;

    public bool shouldQuit { get; private set; }

    public bool shouldRestart { get; private set; }

    public bool shouldConfigure { get; private set; }

    public bool shouldAdvance => isRunning && !shouldQuit;

    public bool processCommand(char command)
    {
        switch (char.ToLowerInvariant(command))
        {
            case 'p':
                isRunning = false;
                return true;
            case 'r':
                if (!shouldQuit)
                {
                    isRunning = true;
                }

                return true;
            case 'q':
                shouldQuit = true;
                isRunning = false;
                return true;
            case 'c':
                shouldConfigure = true;
                isRunning = false;
                return true;
            case 'x':
                shouldRestart = true;
                isRunning = false;
                return true;
            default:
                return false;
        }
    }

    public void completeConfiguration()
    {
        shouldConfigure = false;
    }

    public int getNextStep()
    {
        if (shouldAdvance)
        {
            return nextStep++;
        }

        return nextStep;
    }
}
