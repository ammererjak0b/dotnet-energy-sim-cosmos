namespace EnergySim.Wind;

/// <summary>Configurable electrical power and wind speed thresholds of a wind plant.</summary>
public sealed class WindOptions
{
    public const string SECTION_NAME = "Wind";

    private const double DEFAULT_RATED_POWER_KW = 100.0;
    private const double DEFAULT_CUT_IN_WIND_SPEED_MS = 3.0;
    private const double DEFAULT_RATED_WIND_SPEED_MS = 12.0;
    private const double DEFAULT_CUT_OUT_WIND_SPEED_MS = 25.0;
    private const double DEFAULT_RESTART_WIND_SPEED_MS = 20.0;

    public double RatedPowerKw { get; set; } = DEFAULT_RATED_POWER_KW;

    public double CutInWindSpeedMs { get; set; } = DEFAULT_CUT_IN_WIND_SPEED_MS;

    public double RatedWindSpeedMs { get; set; } = DEFAULT_RATED_WIND_SPEED_MS;

    public double CutOutWindSpeedMs { get; set; } = DEFAULT_CUT_OUT_WIND_SPEED_MS;

    public double RestartWindSpeedMs { get; set; } = DEFAULT_RESTART_WIND_SPEED_MS;

    /// <summary>Rejects invalid wind plant settings before a simulation starts.</summary>
    public void validate()
    {
        if (!double.IsFinite(RatedPowerKw) || RatedPowerKw <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(RatedPowerKw), RatedPowerKw, "RatedPowerKw must be finite and greater than 0.");
        }

        if (!double.IsFinite(CutInWindSpeedMs) || CutInWindSpeedMs < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(CutInWindSpeedMs), CutInWindSpeedMs, "CutInWindSpeedMs must be finite and at least 0.");
        }

        if (!double.IsFinite(RatedWindSpeedMs) || RatedWindSpeedMs <= CutInWindSpeedMs)
        {
            throw new ArgumentOutOfRangeException(nameof(RatedWindSpeedMs), RatedWindSpeedMs, "RatedWindSpeedMs must be finite and greater than CutInWindSpeedMs.");
        }

        if (!double.IsFinite(CutOutWindSpeedMs) || CutOutWindSpeedMs <= RatedWindSpeedMs)
        {
            throw new ArgumentOutOfRangeException(nameof(CutOutWindSpeedMs), CutOutWindSpeedMs, "CutOutWindSpeedMs must be finite and greater than RatedWindSpeedMs.");
        }

        if (!double.IsFinite(RestartWindSpeedMs) || RestartWindSpeedMs <= CutInWindSpeedMs || RestartWindSpeedMs >= CutOutWindSpeedMs)
        {
            throw new ArgumentOutOfRangeException(nameof(RestartWindSpeedMs), RestartWindSpeedMs, "RestartWindSpeedMs must be finite, greater than CutInWindSpeedMs and less than CutOutWindSpeedMs.");
        }
    }
}
