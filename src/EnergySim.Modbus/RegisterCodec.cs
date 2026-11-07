namespace EnergySim.Modbus;

/// <summary>
/// 16-bit register encoding shared by device server and diagnostic client.
/// Byte order big-endian per register (Modbus standard), word order high word first (ABCD).
/// </summary>
public static class RegisterCodec
{
    private const int BITS_PER_REGISTER = 16;

    private const int REGISTER_MASK = 0xFFFF;

    public static void writeUInt32(ushort[] registers, int offset, uint value)
    {
        registers[offset] = (ushort)(value >> BITS_PER_REGISTER);
        registers[offset + 1] = (ushort)(value & REGISTER_MASK);
    }

    public static uint readUInt32(ushort[] registers, int offset)
    {
        uint highWord = registers[offset];
        uint lowWord = registers[offset + 1];
        return (highWord << BITS_PER_REGISTER) | lowWord;
    }

    public static void writeInt32(ushort[] registers, int offset, int value)
    {
        writeUInt32(registers, offset, unchecked((uint)value));
    }

    public static int readInt32(ushort[] registers, int offset)
    {
        return unchecked((int)readUInt32(registers, offset));
    }

    public static void writeUInt64(ushort[] registers, int offset, ulong value)
    {
        writeUInt32(registers, offset, (uint)(value >> (2 * BITS_PER_REGISTER)));
        writeUInt32(registers, offset + 2, (uint)(value & 0xFFFFFFFFUL));
    }

    public static ulong readUInt64(ushort[] registers, int offset)
    {
        ulong highPart = readUInt32(registers, offset);
        ulong lowPart = readUInt32(registers, offset + 2);
        return (highPart << (2 * BITS_PER_REGISTER)) | lowPart;
    }

    /// <summary>value * scale, rounded, clamped to the s32 range. NaN becomes 0.</summary>
    public static int scaleToRaw(double value, int scale)
    {
        if (double.IsNaN(value))
        {
            return 0;
        }

        double scaled = Math.Round(value * scale);
        if (scaled > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (scaled < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)scaled;
    }

    public static double rawToScaled(int raw, int scale)
    {
        return (double)raw / scale;
    }

    /// <summary>EasyModbus returns int per register (client) or short (server array): normalize to ushort.</summary>
    public static ushort toRegister(int value)
    {
        return unchecked((ushort)value);
    }

    public static short toServerShort(ushort value)
    {
        return unchecked((short)value);
    }
}
