using BatteryStorageSimulator.Domain;

namespace BatteryStorageSimulator.Simulation;

internal sealed record SimulationStatus(
    DateTime SimulatedTime,
    DateTime EndTime,
    TimeSpan Step,
    double SpeedFactor,
    SimulationMode Mode,
    bool IsPaused,
    double AutoPowerLimitPercent,
    string LastEvent,
    BatterySnapshot Battery);
