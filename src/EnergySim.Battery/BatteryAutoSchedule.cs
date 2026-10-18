namespace EnergySim.Battery;

/// <summary>
/// Auto mode schedule (UTC hour): 00-06 charge, 06-10 discharge, 10-16 charge, 16-22 discharge, 22-24 charge.
/// Pure function of (seed, virtual minute, SoC): no state, independent of tick rate and call count.
/// </summary>
public static class BatteryAutoSchedule
{
    private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;
    private const int SECONDS_PER_MINUTE = 60;
    private const int MANTISSA_SHIFT = 11; // top 53 bits -> double in [0,1)
    private const double MANTISSA_SCALE = 9007199254740992.0; // 2^53

    /// <summary>Requested power in kW: positive = discharge, negative = charge, 0 when SoC window is exhausted.</summary>
    public static double getRequestedPowerKW(BatteryOptions options, DateTimeOffset virtualNow, double socPercent)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        int hour = virtualNow.UtcDateTime.Hour;
        double factorPosition = getRandomFraction(options.RandomSeed, virtualNow);

        if (hour < 6)
        {
            return chargingPower(options, socPercent, 0.20, 0.50, factorPosition);
        }

        if (hour < 10)
        {
            return dischargingPower(options, socPercent, 0.20, 0.60, factorPosition);
        }

        if (hour < 16)
        {
            return chargingPower(options, socPercent, 0.10, 0.80, factorPosition);
        }

        if (hour < 22)
        {
            return dischargingPower(options, socPercent, 0.30, 1.00, factorPosition);
        }

        return chargingPower(options, socPercent, 0.10, 0.30, factorPosition);
    }

    private static double chargingPower(BatteryOptions options, double socPercent, double minimumFactor, double maximumFactor, double factorPosition)
    {
        if (socPercent >= options.MaximumSocPercent)
        {
            return 0;
        }

        return -options.MaxChargePowerKW * (minimumFactor + factorPosition * (maximumFactor - minimumFactor));
    }

    private static double dischargingPower(BatteryOptions options, double socPercent, double minimumFactor, double maximumFactor, double factorPosition)
    {
        if (socPercent <= options.MinimumSocPercent)
        {
            return 0;
        }

        return options.MaxDischargePowerKW * (minimumFactor + factorPosition * (maximumFactor - minimumFactor));
    }

    /// <summary>Hash of (seed, minute of virtual time) mapped to [0,1). FNV-1a plus finalizer mix.</summary>
    private static double getRandomFraction(int seed, DateTimeOffset virtualNow)
    {
        long minute = (long)Math.Floor(virtualNow.ToUnixTimeSeconds() / (double)SECONDS_PER_MINUTE);

        ulong hash = FNV_OFFSET_BASIS;
        hash = mixIn(hash, (ulong)seed);
        hash = mixIn(hash, (ulong)minute);

        hash ^= hash >> 33; // avalanche
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;

        return (hash >> MANTISSA_SHIFT) / MANTISSA_SCALE;
    }

    private static ulong mixIn(ulong hash, ulong value)
    {
        for (int byteIndex = 0; byteIndex < 8; byteIndex++)
        {
            hash ^= (value >> (byteIndex * 8)) & 0xFF;
            hash *= FNV_PRIME;
        }

        return hash;
    }
}
