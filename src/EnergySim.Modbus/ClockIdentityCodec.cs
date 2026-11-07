using System.Buffers.Binary;

namespace EnergySim.Modbus;

/// <summary>Device ID in the first 18 input registers; ASCII, length-prefixed.</summary>
public static class ClockIdentityCodec
{
    public const int REGISTER_COUNT = 18;
    private const int ID_OFFSET = 4;
    private const int MAX_ID_LENGTH = 32;

    public static int[] encode(string deviceId)
    {
        ClockEndpointOptions options = new ClockEndpointOptions();
        options.Port = 1601;
        options.DeviceId = deviceId;
        options.validate();
        byte[] bytes = new byte[REGISTER_COUNT * 2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, ClockCodec.VERSION);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)deviceId.Length);
        for (int index = 0; index < deviceId.Length; index++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(ID_OFFSET + index * 2), deviceId[index]);
        }

        return ClockCodec.toRegisters(bytes);
    }

    public static string decode(int[] registers)
    {
        if (registers.Length != REGISTER_COUNT || registers[0] != ClockCodec.VERSION
            || registers[1] < 1 || registers[1] > MAX_ID_LENGTH)
        {
            throw new ArgumentException("Invalid device identity header.");
        }

        byte[] bytes = ClockCodec.toBytes(registers);
        int length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(2));
        char[] characters = new char[length];
        for (int index = 0; index < length; index++)
        {
            ushort value = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(ID_OFFSET + index * 2));
            if (value > 127)
            {
                throw new ArgumentException("Device identity must be ASCII.");
            }

            characters[index] = (char)value;
        }

        string deviceId = new string(characters);
        ClockEndpointOptions options = new ClockEndpointOptions();
        options.Port = 1601;
        options.DeviceId = deviceId;
        options.validate();
        return deviceId;
    }
}
