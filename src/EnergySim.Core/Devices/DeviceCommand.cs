namespace EnergySim.Core.Devices;

public sealed class DeviceCommand
{
    public DeviceCommand(string name, double value)
    {
        Name = name;
        Value = value;
        TextValue = string.Empty;
    }

    public DeviceCommand(string name, string textValue)
    {
        Name = name;
        Value = 0;
        TextValue = textValue;
    }

    public string Name { get; }

    public double Value { get; }

    public string TextValue { get; }
}
