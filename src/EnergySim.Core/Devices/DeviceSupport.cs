using System.Globalization;

namespace EnergySim.Core.Devices;

/// <summary>Small builders shared by all device adapters (rows, command descriptors, number format).</summary>
public static class DeviceSupport
{
    public const double FULL_PERCENT = 100.0;

    public static DisplayRow createRow(string label, string value)
    {
        DisplayRow row = new DisplayRow();
        row.Label = label;
        row.Value = value;
        return row;
    }

    public static CommandDescriptor createCommand(string name, char key, string label)
    {
        CommandDescriptor descriptor = new CommandDescriptor();
        descriptor.Name = name;
        descriptor.Key = key;
        descriptor.Label = label;
        return descriptor;
    }

    /// <summary>Command with a numeric parameter (range and unit shown in the prompt).</summary>
    public static CommandDescriptor createParameterCommand(string name, char key, string label, double minimum, double maximum, string unit)
    {
        CommandDescriptor descriptor = createCommand(name, key, label);
        descriptor.HasParameter = true;
        descriptor.Minimum = minimum;
        descriptor.Maximum = maximum;
        descriptor.Unit = unit;
        return descriptor;
    }

    /// <summary>O on, F off, L limit, T fault, R reset: identical on every device.</summary>
    public static List<CommandDescriptor> createCommonCommands()
    {
        List<CommandDescriptor> commands = new List<CommandDescriptor>();
        commands.Add(createCommand(CommonCommands.TURN_ON, CommonCommands.KEY_TURN_ON, "Turn on"));
        commands.Add(createCommand(CommonCommands.TURN_OFF, CommonCommands.KEY_TURN_OFF, "Turn off"));
        commands.Add(createParameterCommand(CommonCommands.SET_LIMIT, CommonCommands.KEY_SET_LIMIT, "Power limit", 0.0, FULL_PERCENT, "%"));
        commands.Add(createCommand(CommonCommands.TRIGGER_FAULT, CommonCommands.KEY_TRIGGER_FAULT, "Trigger fault"));
        commands.Add(createCommand(CommonCommands.RESET_FAULT, CommonCommands.KEY_RESET_FAULT, "Reset fault"));
        return commands;
    }

    public static bool isValidPercent(double percent)
    {
        return !double.IsNaN(percent) && percent >= 0.0 && percent <= FULL_PERCENT;
    }

    public static string format(double value, string format)
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
