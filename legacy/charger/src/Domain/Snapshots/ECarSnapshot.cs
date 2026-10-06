namespace LB_01.src.Domain.Snapshots;

public sealed record ECarSnapshot(
    int id,
    string name,
    string manufacturer,
    string model,
    double batteryCapacityKwh,
    double stateOfChargePercent,
    double maximumChargingPowerKw);
