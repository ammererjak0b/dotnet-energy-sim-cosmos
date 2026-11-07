using EnergySim.Modbus;

namespace EnergySim.Tests.Engine;

/// <summary>Device registration uses a bounded, versioned identity block.</summary>
public sealed class ClockIdentityCodecTests
{
    [Fact]
    public void Encode_Identity_RoundTripsDeviceId()
    {
        // Arrange
        string deviceId = "pv-roof_1";

        // Act
        int[] registers = ClockIdentityCodec.encode(deviceId);
        string decoded = ClockIdentityCodec.decode(registers);

        // Assert
        Assert.Equal(18, registers.Length);
        Assert.Equal(deviceId, decoded);
    }
}
