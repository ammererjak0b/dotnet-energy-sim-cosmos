namespace EnergySim.Modbus;

/// <summary>One device-specific register value: name, unit and scale. Client and server use the same table.</summary>
public sealed class DeviceProfileValue
{
    public DeviceProfileValue(string name, string unit, int scale)
    {
        Name = name;
        Unit = unit;
        Scale = scale; // raw = value * Scale
    }

    public string Name { get; }

    public string Unit { get; }

    public int Scale { get; }
}
