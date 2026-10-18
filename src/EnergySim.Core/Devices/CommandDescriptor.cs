namespace EnergySim.Core.Devices;

/// <summary>A command a device offers. The terminal draws the key legend and validates the parameter.</summary>
public sealed class CommandDescriptor
{
    public string Name { get; set; } = string.Empty; // see CommonCommands for the common ones

    public char Key { get; set; }

    public string Label { get; set; } = string.Empty;

    public bool HasParameter { get; set; }

    public double Minimum { get; set; }

    public double Maximum { get; set; }

    public string Unit { get; set; } = string.Empty;
}
