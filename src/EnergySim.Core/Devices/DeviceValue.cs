namespace EnergySim.Core.Devices;

/// <summary>One device-specific measurement. Order and names must match the profile of the device type in EnergySim.Modbus.</summary>
public sealed class DeviceValue
{
    public DeviceValue(string name, double value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public double Value { get; } // engineering unit, scaling happens in the Modbus layer
}
