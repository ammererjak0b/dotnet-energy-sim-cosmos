using EnergySim.Modbus;

namespace EnergySim.Tests.Engine;

/// <summary>Clock service endpoints remain unprivileged and local by default.</summary>
public sealed class ClockEndpointOptionsTests
{
    [Fact]
    public void Validate_NonLocalHost_Throws()
    {
        // Arrange
        ClockEndpointOptions options = new ClockEndpointOptions();
        options.Host = "192.168.1.2";
        options.Port = 1601;
        options.DeviceId = "pv-1";

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }
}
