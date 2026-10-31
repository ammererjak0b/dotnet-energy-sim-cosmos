using EnergySim.Modbus;

namespace EnergySim.Tests.Engine;

/// <summary>Connecting devices read the service timing policy from a small versioned block.</summary>
public sealed class ClockServiceInfoCodecTests
{
    [Fact]
    public void Encode_ServiceTiming_RoundTripsConfiguration()
    {
        // Arrange
        ClockServiceInfo info = new ClockServiceInfo();
        info.HeartbeatMilliseconds = 1000;
        info.HeartbeatExpirySeconds = 3;
        info.LeadTimeMilliseconds = 100;

        // Act
        int[] registers = ClockServiceInfoCodec.encode(info);
        ClockServiceInfo decoded = ClockServiceInfoCodec.decode(registers);

        // Assert
        Assert.Equal(4, registers.Length);
        Assert.Equal(info.HeartbeatMilliseconds, decoded.HeartbeatMilliseconds);
        Assert.Equal(info.HeartbeatExpirySeconds, decoded.HeartbeatExpirySeconds);
        Assert.Equal(info.LeadTimeMilliseconds, decoded.LeadTimeMilliseconds);
    }
}
