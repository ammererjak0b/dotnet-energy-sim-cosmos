namespace EnergySim.Core.Devices;

public sealed class DeviceCommand
{
    public DeviceCommand(string name, double value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public double Value { get; }
}
