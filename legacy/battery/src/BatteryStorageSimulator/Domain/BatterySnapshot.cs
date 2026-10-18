namespace BatteryStorageSimulator.Domain;

internal sealed record BatterySnapshot(
    string DeviceId,
    string DisplayName,
    string Technology,
    BatteryOperatingState OperatingState,
    BatteryFault Fault,
    double RequestedPowerKW,
    double ActualPowerKW,
    double StoredEnergyKWh,
    double NominalCapacityKWh,
    double SocPercent,
    double TemperatureC,
    double WarningTemperatureC,
    double CumulativeChargedEnergyKWh,
    double CumulativeDischargedEnergyKWh);
