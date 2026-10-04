using BatteryStorageSimulator.Configuration;
using BatteryStorageSimulator.Domain;

namespace BatteryStorageSimulator.Simulation;

internal sealed class AutoPowerController
{
    private readonly BatteryOptions options;
    private readonly Random random;

    public AutoPowerController(BatteryOptions options, int randomSeed)
    {
        this.options = options;
        random = new Random(randomSeed);
    }

    public double GetRequestedPowerKW(
        DateTime simulatedTime,
        BatterySnapshot battery,
        double autoPowerLimitPercent)
    {
        var limitFactor = autoPowerLimitPercent / 100.0;
        var hour = simulatedTime.Hour;

        if (hour < 6)
        {
            return ChargingPower(0.20, 0.50, limitFactor, battery);
        }

        if (hour < 10)
        {
            return DischargingPower(0.20, 0.60, limitFactor, battery);
        }

        if (hour < 16)
        {
            return ChargingPower(0.10, 0.80, limitFactor, battery);
        }

        if (hour < 22)
        {
            return DischargingPower(0.30, 1.00, limitFactor, battery);
        }

        return ChargingPower(0.10, 0.30, limitFactor, battery);
    }

    private double ChargingPower(
        double minimumFactor,
        double maximumFactor,
        double limitFactor,
        BatterySnapshot battery)
    {
        if (battery.SocPercent >= options.MaximumSocPercent)
        {
            return 0;
        }

        return -options.MaxChargePowerKW * limitFactor * RandomFactor(minimumFactor, maximumFactor);
    }

    private double DischargingPower(
        double minimumFactor,
        double maximumFactor,
        double limitFactor,
        BatterySnapshot battery)
    {
        if (battery.SocPercent <= options.MinimumSocPercent)
        {
            return 0;
        }

        return options.MaxDischargePowerKW * limitFactor * RandomFactor(minimumFactor, maximumFactor);
    }

    private double RandomFactor(double minimum, double maximum) =>
        minimum + random.NextDouble() * (maximum - minimum);
}
