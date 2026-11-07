using EnergySim.Modbus;

namespace EnergySim.Tests.Engine;

/// <summary>Heartbeat traffic remains small and does not lose issue time.</summary>
public sealed class HeartbeatCodecTests
{
    [Fact]
    public void Encode_Heartbeat_RoundTripsInSixteenRegisters()
    {
        // Arrange
        ClockHeartbeat heartbeat = new ClockHeartbeat();
        heartbeat.SessionId = Guid.NewGuid();
        heartbeat.Revision = 17;
        heartbeat.IssuedTimestamp = 9007199254740993;

        // Act
        int[] registers = HeartbeatCodec.encode(heartbeat);
        ClockHeartbeat decoded = HeartbeatCodec.decode(registers);

        // Assert
        Assert.Equal(16, registers.Length);
        Assert.Equal(heartbeat.SessionId, decoded.SessionId);
        Assert.Equal(heartbeat.Revision, decoded.Revision);
        Assert.Equal(heartbeat.IssuedTimestamp, decoded.IssuedTimestamp);
    }
}
