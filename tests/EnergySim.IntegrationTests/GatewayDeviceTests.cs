using EnergySim.Core.Devices;
using EnergySim.Gateway;
using EnergySim.Messaging;

namespace EnergySim.IntegrationTests;

/// <summary>
/// Gateway device against a real device Modbus server: what the gateway publishes for a new state, an unchanged state,
/// a wrong device type and a stopped device. No broker needed, the gateway device only returns the message to publish.
/// Expected values as in DeviceModbusServerTests (PV formula, contract docs/contracts/messaging.md).
/// </summary>
[Trait("Category", "Integration")]
public sealed class GatewayDeviceTests : PvServerTestBase
{
    private static readonly DateTime POLLED_AT = new DateTime(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private GatewayDevice? _gateway;

    /// <summary>Runs also when a test failed.</summary>
    public override Task DisposeAsync()
    {
        if (_gateway != null)
        {
            _gateway.Dispose();
        }

        return base.DisposeAsync();
    }

    [Fact]
    public void Poll_NewState_ReturnsGoodMessageWithPvValues()
    {
        // Arrange
        PvServerHarness harness = getHarness();
        harness.publishMiddayFrame(7);
        GatewayDevice gateway = createGateway(DeviceType.Pv);

        // Act
        TelemetryMessage? message = gateway.poll(POLLED_AT, CancellationToken.None);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("pv-1", message.DeviceId);
        Assert.Equal("Pv", message.DeviceType);
        Assert.Equal(TelemetryQuality.Good, message.Quality);
        Assert.Equal(7u, message.Sequence);
        Assert.Equal(PvServerHarness.VirtualTime.UtcDateTime, message.SimTimeUtc);
        Assert.Equal(POLLED_AT, message.PolledAtUtc);
        Assert.Equal("Running", message.State);
        Assert.Equal(PvServerHarness.POWER_W, findValue(message, "ActivePower", "W"));
        Assert.Equal(800.0, findValue(message, "Irradiance", "W/m2"));
        Assert.Equal(40.0, findValue(message, "ModuleTemperature", "degC"), 6);
    }

    [Fact]
    public void Poll_SameStateTwice_ReturnsNothingNewTheSecondTime()
    {
        // Arrange
        getHarness().publishMiddayFrame(7);
        GatewayDevice gateway = createGateway(DeviceType.Pv);
        TelemetryMessage? first = gateway.poll(POLLED_AT, CancellationToken.None);

        // Act
        TelemetryMessage? second = gateway.poll(POLLED_AT.AddSeconds(1), CancellationToken.None);

        // Assert: only a new sequence or state is published, no spam while paused
        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public void Poll_NewSequence_ReturnsNewMessage()
    {
        // Arrange
        PvServerHarness harness = getHarness();
        harness.publishMiddayFrame(7);
        GatewayDevice gateway = createGateway(DeviceType.Pv);
        gateway.poll(POLLED_AT, CancellationToken.None);

        // Act
        harness.publishMiddayFrame(8);
        TelemetryMessage? message = gateway.poll(POLLED_AT.AddSeconds(1), CancellationToken.None);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(8u, message.Sequence);
    }

    [Fact]
    public void Poll_ConfiguredTypeDiffersFromDevice_ReturnsBadOnce()
    {
        // Arrange
        getHarness().publishMiddayFrame(7);
        GatewayDevice gateway = createGateway(DeviceType.Wind); // the server is a PV

        // Act
        TelemetryMessage? first = gateway.poll(POLLED_AT, CancellationToken.None);
        TelemetryMessage? second = gateway.poll(POLLED_AT.AddSeconds(1), CancellationToken.None);

        // Assert
        Assert.NotNull(first);
        Assert.Equal(TelemetryQuality.Bad, first.Quality);
        Assert.Contains("device reports type Pv, configured Wind", first.Error);
        Assert.Empty(first.Measurements);
        Assert.Null(first.SimTimeUtc);
        Assert.Null(second); // Bad is sent once per outage
    }

    [Fact]
    public void Poll_DeviceStopped_ReturnsBadOnce()
    {
        // Arrange
        PvServerHarness harness = getHarness();
        harness.publishMiddayFrame(7);
        GatewayDevice gateway = createGateway(DeviceType.Pv);
        TelemetryMessage? good = gateway.poll(POLLED_AT, CancellationToken.None);

        // Act
        harness.Dispose(); // device process ends
        TelemetryMessage? firstBad = gateway.poll(POLLED_AT.AddSeconds(1), CancellationToken.None);
        TelemetryMessage? secondBad = gateway.poll(POLLED_AT.AddSeconds(2), CancellationToken.None);

        // Assert
        Assert.NotNull(good);
        Assert.NotNull(firstBad);
        Assert.Equal(TelemetryQuality.Bad, firstBad.Quality);
        Assert.False(string.IsNullOrEmpty(firstBad.Error));
        Assert.Empty(firstBad.Measurements); // no stale values after an outage
        Assert.Null(firstBad.SimTimeUtc);
        Assert.Null(secondBad);
    }

    private GatewayDevice createGateway(DeviceType type)
    {
        GatewayDeviceConfig config = new GatewayDeviceConfig("pv-1", type, PvServerHarness.HOST, getHarness().Port);
        _gateway = new GatewayDevice(config, PvServerHarness.UNIT_ID);
        return _gateway;
    }

    private static double findValue(TelemetryMessage message, string name, string unit)
    {
        foreach (Measurement measurement in message.Measurements)
        {
            if (measurement.Name == name)
            {
                Assert.Equal(unit, measurement.Unit);
                return measurement.Value;
            }
        }

        throw new Xunit.Sdk.XunitException("measurement " + name + " missing");
    }
}
