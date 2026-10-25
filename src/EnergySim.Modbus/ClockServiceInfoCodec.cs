namespace EnergySim.Modbus;

/// <summary>Encodes the versioned service timing policy as four Modbus registers.</summary>
public static class ClockServiceInfoCodec
{
    public const int REGISTER_COUNT = 4;
    public const int REGISTER_ADDRESS = 100;
    private const int VERSION = 1;

    public static int[] encode(ClockServiceInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        validate(info);
        return new int[]
        {
            VERSION,
            info.HeartbeatMilliseconds,
            info.HeartbeatExpirySeconds,
            info.LeadTimeMilliseconds
        };
    }

    public static ClockServiceInfo decode(IReadOnlyList<int> registers)
    {
        ArgumentNullException.ThrowIfNull(registers);
        if (registers.Count != REGISTER_COUNT || registers[0] != VERSION)
        {
            throw new ArgumentException("Invalid time service information block.", nameof(registers));
        }

        ClockServiceInfo info = new ClockServiceInfo();
        info.HeartbeatMilliseconds = registers[1];
        info.HeartbeatExpirySeconds = registers[2];
        info.LeadTimeMilliseconds = registers[3];
        validate(info);
        return info;
    }

    private static void validate(ClockServiceInfo info)
    {
        if (info.HeartbeatMilliseconds < 250 || info.HeartbeatExpirySeconds < 2
            || info.LeadTimeMilliseconds < 25)
        {
            throw new ArgumentOutOfRangeException(nameof(info));
        }
    }
}
