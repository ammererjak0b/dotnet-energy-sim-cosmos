namespace LB_01.Domain.Entities;

public class ECar
{
    public string Id { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public double BatteryCapacityKwh { get; init; }

    public double StateOfChargePercent { get; set; }

    public double MaximumChargingPowerKw { get; init; }
}
