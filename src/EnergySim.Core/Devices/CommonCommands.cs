namespace EnergySim.Core.Devices;

/// <summary>Command names and keys every device understands. Same keys on all devices, one place.</summary>
public static class CommonCommands
{
    public const string TURN_ON = "TurnOn";

    public const string TURN_OFF = "TurnOff";

    public const string SET_LIMIT = "SetLimit"; // value in percent 0..100

    public const string RESET_FAULT = "ResetFault";

    public const string TRIGGER_FAULT = "TriggerFault"; // manual fault injection

    public const char KEY_TURN_ON = 'O';

    public const char KEY_TURN_OFF = 'F';

    public const char KEY_SET_LIMIT = 'L';

    public const char KEY_TRIGGER_FAULT = 'T';

    public const char KEY_RESET_FAULT = 'R';
}
