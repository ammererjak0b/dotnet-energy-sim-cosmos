using EnergySim.Modbus;

namespace EnergySim.UnitTests.Engine;

/// <summary>Clock endpoints: unprivileged ports, loopback by default, other hosts only by explicit option (containers).</summary>
public sealed class ClockEndpointOptionsTests
{
    private static ClockEndpointOptions createOptions(string host)
    {
        ClockEndpointOptions options = new ClockEndpointOptions();
        options.Host = host;
        options.Port = 1601;
        options.DeviceId = "pv-1";
        return options;
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("localhost")]
    [InlineData("0.0.0.0")] // container: reachable from the Docker network
    [InlineData("192.168.1.2")]
    public void Validate_LocalOrIpHost_Accepted(string host)
    {
        // Arrange
        ClockEndpointOptions options = createOptions(host);

        // Act
        options.validate();

        // Assert
        Assert.NotNull(options.bindAddress());
    }

    [Theory]
    [InlineData("")]
    [InlineData("wind-1")] // bind needs an IP, a host name is only for dialing
    [InlineData("not an ip")]
    public void Validate_NoIpAddress_Throws(string host)
    {
        // Arrange
        ClockEndpointOptions options = createOptions(host);

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }

    [Fact]
    public void BindAddress_DefaultAndLocalhost_IsLoopback()
    {
        // Arrange
        ClockEndpointOptions byDefault = new ClockEndpointOptions();
        ClockEndpointOptions byName = createOptions("localhost");

        // Act
        System.Net.IPAddress defaultAddress = byDefault.bindAddress();
        System.Net.IPAddress nameAddress = byName.bindAddress();

        // Assert
        Assert.Equal(System.Net.IPAddress.Loopback, defaultAddress);
        Assert.Equal(System.Net.IPAddress.Loopback, nameAddress);
    }

    [Theory]
    [InlineData("master")]
    [InlineData("192.168.1.4")]
    [InlineData("127.0.0.1")]
    public void SharedConnection_ServiceHost_AcceptsNameOrIp(string host)
    {
        // Arrange
        SharedClockConnectionOptions options = new SharedClockConnectionOptions();
        options.ServiceHost = host;

        // Act
        options.validate();

        // Assert
        Assert.Equal(host, options.ServiceHost);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void SharedConnection_EmptyServiceHost_Throws(string host)
    {
        // Arrange
        SharedClockConnectionOptions options = new SharedClockConnectionOptions();
        options.ServiceHost = host;

        // Act
        Action validate = delegate { options.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }
}
