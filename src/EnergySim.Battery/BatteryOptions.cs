namespace EnergySim.Battery;

/// <summary>Battery parameters (config section "Battery"). Defaults = LB01 batterysettings.json.</summary>
public sealed class BatteryOptions
{
    public const string SECTION_NAME = "Battery";

    public string Technology { get; set; } = "LFP";

    public double CapacityKWh { get; set; } = 100.0;

    public double MaxChargePowerKW { get; set; } = 50.0;

    public double MaxDischargePowerKW { get; set; } = 50.0;

    public double InitialSocPercent { get; set; } = 50.0;

    public double MinimumSocPercent { get; set; } = 10.0;

    public double MaximumSocPercent { get; set; } = 90.0;

    public double ChargeEfficiencyPercent { get; set; } = 95.0;

    public double DischargeEfficiencyPercent { get; set; } = 95.0;

    public double InitialTemperatureC { get; set; } = 25.0;

    public double AmbientTemperatureC { get; set; } = 22.0;

    public double WarningTemperatureC { get; set; } = 40.0;

    public double FaultTemperatureC { get; set; } = 50.0;

    public double ResetTemperatureC { get; set; } = 40.0; // fault reset only at or below

    public int RandomSeed { get; set; } = 42; // auto mode

    /// <summary>Throws ArgumentOutOfRangeException with a readable message on invalid values.</summary>
    public void validate()
    {
        if (string.IsNullOrWhiteSpace(Technology))
        {
            throw new ArgumentOutOfRangeException(nameof(Technology), "Technology must not be empty.");
        }

        requirePositive(CapacityKWh, nameof(CapacityKWh));
        requirePositive(MaxChargePowerKW, nameof(MaxChargePowerKW));
        requirePositive(MaxDischargePowerKW, nameof(MaxDischargePowerKW));

        if (double.IsNaN(MinimumSocPercent) || double.IsNaN(MaximumSocPercent) || MinimumSocPercent < 0 || MaximumSocPercent > 100 || MinimumSocPercent >= MaximumSocPercent)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumSocPercent), "SoC window must satisfy 0 <= min < max <= 100.");
        }

        if (double.IsNaN(InitialSocPercent) || InitialSocPercent < MinimumSocPercent || InitialSocPercent > MaximumSocPercent)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialSocPercent), "Initial SoC must be inside the SoC window.");
        }

        requireEfficiency(ChargeEfficiencyPercent, nameof(ChargeEfficiencyPercent));
        requireEfficiency(DischargeEfficiencyPercent, nameof(DischargeEfficiencyPercent));

        requireFinite(InitialTemperatureC, nameof(InitialTemperatureC));
        requireFinite(AmbientTemperatureC, nameof(AmbientTemperatureC));
        requireFinite(WarningTemperatureC, nameof(WarningTemperatureC));
        requireFinite(FaultTemperatureC, nameof(FaultTemperatureC));
        requireFinite(ResetTemperatureC, nameof(ResetTemperatureC));

        if (ResetTemperatureC >= FaultTemperatureC)
        {
            throw new ArgumentOutOfRangeException(nameof(ResetTemperatureC), "ResetTemperatureC must be below FaultTemperatureC.");
        }

        if (WarningTemperatureC > FaultTemperatureC)
        {
            throw new ArgumentOutOfRangeException(nameof(WarningTemperatureC), "WarningTemperatureC must not exceed FaultTemperatureC.");
        }
    }

    private static void requirePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, name + " must be greater than 0.");
        }
    }

    private static void requireEfficiency(double value, string name)
    {
        if (double.IsNaN(value) || value <= 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(name, value, name + " must be in (0, 100].");
        }
    }

    private static void requireFinite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, value, name + " must be a finite number.");
        }
    }
}
