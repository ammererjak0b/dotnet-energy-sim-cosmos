namespace LB_01.Domain.Entities;

public class ECar
{
    public int id { get; init; }

    public string name { get; init; } = string.Empty;

    public string manufacturer { get; init; } = string.Empty;

    public string model { get; init; } = string.Empty;

    public double batteryCapacityKwh { get; init; }

    public double stateOfChargePercent { get; set; }

    public double maximumChargingPowerKw { get; init; }

    public ChargePort? connectedPort { get; internal set; }
}
