namespace EnergySim.Pv;

/// <summary>Plant settings, bound from config section "Pv". Defaults match a small roof plant.</summary>
public class PvOptions
{
    public const string SECTION_NAME = "Pv";

    public double PeakPowerKw { get; set; } = 5.0; // kWp at 1000 W/m2 and 25 C

    public double PerformanceRatio { get; set; } = 0.8; // losses (inverter, cable, dirt) as one factor, 0..1

    /// <summary>Throws ArgumentOutOfRangeException with the property name when a value is invalid.</summary>
    public void validate()
    {
        if (double.IsNaN(PeakPowerKw) || PeakPowerKw <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PeakPowerKw), PeakPowerKw, "PeakPowerKw must be greater than 0.");
        }

        if (double.IsNaN(PerformanceRatio) || PerformanceRatio <= 0.0 || PerformanceRatio > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(PerformanceRatio), PerformanceRatio, "PerformanceRatio must be in the range (0, 1].");
        }
    }
}
