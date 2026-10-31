using System.Buffers.Binary;
using EnergySim.Core.Engine;

namespace EnergySim.Modbus;

/// <summary>Fixed big-endian protocol v1; integers retain all 64 bits.</summary>
public static class ClockCodec
{
    public const int VERSION = 1;
    public const int REGISTER_COUNT = 32;
    private const int BYTES_PER_REGISTER = 2;
    private const int SESSION_OFFSET = 4;
    private const int GENERATION_OFFSET = 20;
    private const int REVISION_OFFSET = 24;
    private const int FREQUENCY_OFFSET = 28;
    private const int ANCHOR_OFFSET = 36;
    private const int VIRTUAL_OFFSET = 44;
    private const int SPEED_OFFSET = 52;
    private const int STATE_OFFSET = 60;

    /// <summary>Serializes one definition as an FC16 payload.</summary>
    public static int[] encode(ClockDefinition definition, ClockMessageKind kind)
    {
        if (kind != ClockMessageKind.Initialize && kind != ClockMessageKind.Change && kind != ClockMessageKind.Restart)
        {
            throw new ArgumentException("Invalid clock definition message kind.", nameof(kind));
        }

        definition.validate();
        byte[] bytes = new byte[REGISTER_COUNT * BYTES_PER_REGISTER];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, VERSION);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(BYTES_PER_REGISTER), (ushort)kind);
        definition.SessionId.TryWriteBytes(bytes.AsSpan(SESSION_OFFSET), true, out int written);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(GENERATION_OFFSET), definition.Generation);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(REVISION_OFFSET), definition.Revision);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(FREQUENCY_OFFSET), definition.CounterFrequency);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(ANCHOR_OFFSET), definition.AnchorTimestamp);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(VIRTUAL_OFFSET), definition.VirtualAnchor.UtcTicks);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(SPEED_OFFSET), BitConverter.DoubleToInt64Bits(definition.Speed));
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(STATE_OFFSET), (ushort)definition.Status);
        return toRegisters(bytes);
    }

    /// <summary>Decodes and validates an entire definition before installation.</summary>
    public static ClockDefinition decode(int[] registers, out ClockMessageKind kind)
    {
        if (registers.Length != REGISTER_COUNT || registers[0] != VERSION || registers[REGISTER_COUNT - 1] != 0)
        {
            throw new ArgumentException("Unsupported clock protocol header or length.");
        }

        byte[] bytes = toBytes(registers);
        kind = (ClockMessageKind)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(BYTES_PER_REGISTER));
        if (kind != ClockMessageKind.Initialize && kind != ClockMessageKind.Change && kind != ClockMessageKind.Restart)
        {
            throw new ArgumentException("Unknown clock message kind.");
        }

        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid(bytes.AsSpan(SESSION_OFFSET, 16), true);
        definition.Generation = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(GENERATION_OFFSET));
        definition.Revision = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(REVISION_OFFSET));
        definition.CounterFrequency = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(FREQUENCY_OFFSET));
        definition.AnchorTimestamp = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(ANCHOR_OFFSET));
        definition.VirtualAnchor = new DateTimeOffset(BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(VIRTUAL_OFFSET)), TimeSpan.Zero);
        definition.Speed = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(SPEED_OFFSET)));
        definition.Status = (ClockStatus)BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(STATE_OFFSET));
        definition.validate();
        return definition;
    }

    /// <summary>Converts network bytes to unsigned register values.</summary>
    public static int[] toRegisters(byte[] bytes)
    {
        int[] registers = new int[bytes.Length / BYTES_PER_REGISTER];
        for (int index = 0; index < registers.Length; index++)
        {
            registers[index] = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(index * BYTES_PER_REGISTER));
        }

        return registers;
    }

    /// <summary>Converts register values to network bytes.</summary>
    public static byte[] toBytes(int[] registers)
    {
        byte[] bytes = new byte[registers.Length * BYTES_PER_REGISTER];
        for (int index = 0; index < registers.Length; index++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(index * BYTES_PER_REGISTER), checked((ushort)registers[index]));
        }

        return bytes;
    }
}
