using EnergySim.Core.Devices;
using EnergySim.Core.Engine;

namespace EnergySim.Cli;

/// <summary>Keys that control the clock, not the device (handled by ClockDashboardSource). Q (quit) lives in the dashboard.</summary>
public static class ClockCommands
{
    public const string PAUSE = "ClockPause"; // toggles pause / resume
    public const string SET_SPEED = "ClockSetSpeed";
    public const string FASTER = "ClockFaster";
    public const string SLOWER = "ClockSlower";

    public const char KEY_PAUSE = 'P';
    public const char KEY_SET_SPEED = 'V';
    public const char KEY_FASTER = '+';
    public const char KEY_SLOWER = '-';

    private const double SPEED_STEP_FACTOR = 2.0;

    public static List<CommandDescriptor> createDescriptors()
    {
        List<CommandDescriptor> commands = new List<CommandDescriptor>();
        commands.Add(DeviceSupport.createCommand(PAUSE, KEY_PAUSE, "Pause/resume"));
        commands.Add(DeviceSupport.createParameterCommand(SET_SPEED, KEY_SET_SPEED, "Set speed", LocalClock.MIN_SPEED, LocalClock.MAX_SPEED, "x"));
        commands.Add(DeviceSupport.createCommand(FASTER, KEY_FASTER, "Speed x2"));
        commands.Add(DeviceSupport.createCommand(SLOWER, KEY_SLOWER, "Speed /2"));
        return commands;
    }

    public static bool isClockCommand(string name)
    {
        return name == PAUSE || name == SET_SPEED || name == FASTER || name == SLOWER;
    }

    public static double fasterFactor()
    {
        return SPEED_STEP_FACTOR;
    }

    public static double slowerFactor()
    {
        return 1.0 / SPEED_STEP_FACTOR;
    }

    /// <summary>current * factor, kept inside the clock limits.</summary>
    public static double nextSpeed(double current, double factor)
    {
        return Math.Clamp(current * factor, LocalClock.MIN_SPEED, LocalClock.MAX_SPEED);
    }
}
