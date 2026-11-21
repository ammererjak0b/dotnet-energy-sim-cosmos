using EnergySim.Messaging;

namespace EnergySim.UnitTests.Messaging;

/// <summary>Exchange and routing keys as in docs/contracts/messaging.md.</summary>
public sealed class RoutingTests
{
    [Theory]
    [InlineData("Pv", "pv-1", "telemetry.pv.pv-1")]
    [InlineData("Battery", "battery-1", "telemetry.battery.battery-1")]
    [InlineData("Wind", "wind-2", "telemetry.wind.wind-2")]
    public void RoutingKey_TypeAndId_IsLowerCaseTypeThenId(string deviceType, string deviceId, string expected)
    {
        // Act
        string key = Routing.routingKey(deviceType, deviceId);

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Defaults_ExchangeAndBinding_MatchContract()
    {
        // Assert
        Assert.Equal("energysim.telemetry", Routing.DEFAULT_EXCHANGE);
        Assert.Equal("telemetry.#", Routing.BIND_ALL);
    }
}
