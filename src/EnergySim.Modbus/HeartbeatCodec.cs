using System.Buffers.Binary;

namespace EnergySim.Modbus;

/// <summary>Encodes the 16-register protocol v1 heartbeat.</summary>
public static class HeartbeatCodec
{
    public const int REGISTER_COUNT = 16;
    private const int SESSION_OFFSET = 4;
    private const int REVISION_OFFSET = 20;
    private const int TIMESTAMP_OFFSET = 24;

    public static int[] encode(ClockHeartbeat heartbeat)
    {
        byte[] bytes = new byte[REGISTER_COUNT * 2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, ClockCodec.VERSION);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(2), (ushort)ClockMessageKind.Heartbeat);
        heartbeat.SessionId.TryWriteBytes(bytes.AsSpan(SESSION_OFFSET), true, out int written);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(REVISION_OFFSET), heartbeat.Revision);
        BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(TIMESTAMP_OFFSET), heartbeat.IssuedTimestamp);
        return ClockCodec.toRegisters(bytes);
    }

    public static ClockHeartbeat decode(int[] registers)
    {
        if (registers.Length != REGISTER_COUNT || registers[0] != ClockCodec.VERSION
            || registers[1] != (int)ClockMessageKind.Heartbeat)
        {
            throw new ArgumentException("Invalid heartbeat header or length.");
        }

        byte[] bytes = ClockCodec.toBytes(registers);
        ClockHeartbeat heartbeat = new ClockHeartbeat();
        heartbeat.SessionId = new Guid(bytes.AsSpan(SESSION_OFFSET, 16), true);
        heartbeat.Revision = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(REVISION_OFFSET));
        heartbeat.IssuedTimestamp = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(TIMESTAMP_OFFSET));
        return heartbeat;
    }
}
