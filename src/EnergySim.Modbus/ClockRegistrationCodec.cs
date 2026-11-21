namespace EnergySim.Modbus;

/// <summary>
/// Registration write of a device at the time service, holding register 0.
/// New form: port + device identity (19 registers), so the master knows the id before it dials back.
/// Old form: the port alone (1 register), the master then dials loopback and reads the id from the device.
/// </summary>
public static class ClockRegistrationCodec
{
    public const int REGISTRATION_ADDRESS = 0;
    public const int LEGACY_REGISTER_COUNT = 1;
    public const int REGISTER_COUNT = 1 + ClockIdentityCodec.REGISTER_COUNT;

    public static int[] encode(int port, string deviceId)
    {
        int[] identity = ClockIdentityCodec.encode(deviceId);
        int[] registers = new int[REGISTER_COUNT];
        registers[0] = port;
        for (int index = 0; index < identity.Length; index++)
        {
            registers[1 + index] = identity[index];
        }

        return registers;
    }

    /// <summary>Port and advertised id (empty for the old one-register form). Throws ArgumentException for any other length or a broken identity.</summary>
    public static ClockRegistration decode(IReadOnlyList<int> registers)
    {
        if (registers.Count == LEGACY_REGISTER_COUNT)
        {
            return new ClockRegistration(registers[0], string.Empty);
        }

        if (registers.Count != REGISTER_COUNT)
        {
            throw new ArgumentException("Registration must have " + LEGACY_REGISTER_COUNT + " or " + REGISTER_COUNT + " registers, got " + registers.Count + ".");
        }

        int[] identity = new int[ClockIdentityCodec.REGISTER_COUNT];
        for (int index = 0; index < identity.Length; index++)
        {
            identity[index] = registers[1 + index];
        }

        return new ClockRegistration(registers[0], ClockIdentityCodec.decode(identity));
    }
}
