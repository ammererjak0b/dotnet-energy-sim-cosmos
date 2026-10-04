namespace LB_01.Domain.Entities;

public class ECar
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public double BatteryCapacityKwh { get; init; }

    public double StateOfChargePercent { get; set; }

    public double MaximumChargingPowerKw { get; init; }

    public ChargePort? ConnectedPort { get; internal set; }
}
