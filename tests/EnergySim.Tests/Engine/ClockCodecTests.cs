using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Tests.Engine;

/// <summary>Wire representation preserves counters and virtual time exactly.</summary>
public sealed class ClockCodecTests
{
    [Theory]
    [InlineData(0, 99)]
    [InlineData(1, 99)]
    [InlineData(31, 1)]
    public void Decode_UnsupportedHeader_RejectsMessage(int index, int value)
    {
        // Arrange
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = 1000;
        int[] registers = ClockCodec.encode(definition, ClockMessageKind.Initialize);
        registers[index] = value;

        // Act
        Action decode = delegate { ClockCodec.decode(registers, out ClockMessageKind kind); };

        // Assert
        Assert.Throws<ArgumentException>(decode);
    }

    [Fact]
    public void Encode_HeartbeatKindAsClockDefinition_Throws()
    {
        // Arrange
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = 1000;

        // Act
        Action encode = delegate { ClockCodec.encode(definition, ClockMessageKind.Heartbeat); };

        // Assert
        Assert.Throws<ArgumentException>(encode);
    }

    [Fact]
    public void Encode_ClockDefinition_RoundTripsWithoutPrecisionLoss()
    {
        // Arrange
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = new Guid("11223344-5566-7788-99aa-bbccddeeff00");
        definition.CounterFrequency = 10000000;
        definition.AnchorTimestamp = 9007199254740993;
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch.AddTicks(1234567);
        definition.Speed = 0.125;
        definition.Status = ClockStatus.Paused;

        // Act
        int[] registers = ClockCodec.encode(definition, ClockMessageKind.Initialize);
        ClockDefinition decoded = ClockCodec.decode(registers, out ClockMessageKind kind);

        // Assert
        Assert.Equal(32, registers.Length);
        Assert.Equal(0x1122, registers[2]);
        Assert.Equal(ClockMessageKind.Initialize, kind);
        Assert.Equal(definition.SessionId, decoded.SessionId);
        Assert.Equal(definition.AnchorTimestamp, decoded.AnchorTimestamp);
        Assert.Equal(definition.VirtualAnchor, decoded.VirtualAnchor);
        Assert.Equal(definition.Speed, decoded.Speed);
        Assert.Equal(definition.Status, decoded.Status);
    }
}
