using System.Buffers.Binary;

namespace EnergySim.Modbus;

/// <summary>
/// Device virtual time in the clock endpoint input registers 18..27 (extension of protocol v1, old readers ignore it).
/// Layout: [0] sequence, [1..4] virtual unix ms i64, [5..8] sampled monotonic counter i64, [9] sequence again.
/// A reader accepts the block only when both sequence registers match (no torn read).
/// </summary>
public static class ClockVirtualTimeCodec
{
    public const int REGISTER_ADDRESS = 18;
    public const int REGISTER_COUNT = 10;
    private const int VIRTUAL_OFFSET = 2; // bytes
    private const int TIMESTAMP_OFFSET = 10; // bytes
    private const int LAST_REGISTER = REGISTER_COUNT - 1;
    private const int REGISTER_MASK = 0xFFFF; // 16 bit register

    public static int[] encode(ClockVirtualTimeSample sample, int sequence)
    {
        byte[] bytes = new byte[REGISTER_COUNT * 2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)sequence);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(VIRTUAL_OFFSET), sample.VirtualUnixMilliseconds);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(TIMESTAMP_OFFSET), sample.SampledTimestamp);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(LAST_REGISTER * 2), (ushort)sequence);
        return ClockCodec.toRegisters(bytes);
    }

    /// <summary>Returns false for a torn read (sequence registers differ).</summary>
    public static bool tryDecode(int[] registers, out ClockVirtualTimeSample sample)
    {
        sample = new ClockVirtualTimeSample();
        if (registers.Length != REGISTER_COUNT || (registers[0] & REGISTER_MASK) != (registers[LAST_REGISTER] & REGISTER_MASK))
        {
            return false;
        }

        int[] unsignedRegisters = new int[registers.Length];
        for (int index = 0; index < registers.Length; index++)
        {
            unsignedRegisters[index] = registers[index] & REGISTER_MASK; // client may return signed values
        }

        byte[] bytes = ClockCodec.toBytes(unsignedRegisters);
        sample.VirtualUnixMilliseconds = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(VIRTUAL_OFFSET));
        sample.SampledTimestamp = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(TIMESTAMP_OFFSET));
        return true;
    }
}
