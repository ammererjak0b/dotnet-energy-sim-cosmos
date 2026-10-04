namespace LB_01.Domain.Snapshots;

public sealed record ECarSnapshot(
    int Id,
    string Name,
    string Manufacturer,
    string Model,
    double BatteryCapacityKwh,
    double StateOfChargePercent,
    double MaximumChargingPowerKw);
