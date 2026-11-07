namespace EnergySim.Charger;

/// <summary>Electric car on a port. The charger changes only the SoC.</summary>
public sealed class ECar
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double CapacityKwh { get; set; }

    public double StateOfChargePercent { get; set; }

    public double MaxPowerKw { get; set; } // car side charging limit
}
