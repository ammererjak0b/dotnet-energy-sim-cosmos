namespace EnergySim.Messaging;

/// <summary>One named value with unit.</summary>
public sealed class Measurement
{
    public string Name { get; set; } = string.Empty;

    public double Value { get; set; }

    public string Unit { get; set; } = string.Empty;
}
