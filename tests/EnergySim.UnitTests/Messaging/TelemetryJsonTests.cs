using System.Text;
using System.Text.Json;
using EnergySim.Messaging;

namespace EnergySim.UnitTests.Messaging;

/// <summary>JSON body of a telemetry message as in docs/contracts/messaging.md.</summary>
public sealed class TelemetryJsonTests
{
    private static TelemetryMessage createMessage()
    {
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = "pv-1";
        message.DeviceType = "Pv";
        message.SimTimeUtc = new DateTime(2026, 6, 21, 6, 30, 0, DateTimeKind.Utc);
        message.PolledAtUtc = new DateTime(2026, 11, 12, 10, 15, 3, DateTimeKind.Utc);
        message.Sequence = 1842;
        message.ClockState = "Running";
        message.State = "Running";
        message.Quality = TelemetryQuality.Good;
        Measurement power = new Measurement();
        power.Name = "ActivePower";
        power.Value = 3120;
        power.Unit = "W";
        message.Measurements.Add(power);
        return message;
    }

    [Fact]
    public void Serialize_Message_UsesCamelCaseAndEnumAsText()
    {
        // Arrange
        TelemetryMessage message = createMessage();

        // Act
        string json = Encoding.UTF8.GetString(TelemetryJson.serialize(message));

        // Assert
        Assert.Contains("\"schemaVersion\":1", json);
        Assert.Contains("\"deviceId\":\"pv-1\"", json);
        Assert.Contains("\"simTimeUtc\":\"2026-06-21T06:30:00Z\"", json);
        Assert.Contains("\"quality\":\"Good\"", json);
        Assert.Contains("\"measurements\":[{\"name\":\"ActivePower\",\"value\":3120,\"unit\":\"W\"}]", json);
    }

    [Fact]
    public void Deserialize_SerializedMessage_RoundTrips()
    {
        // Arrange
        TelemetryMessage message = createMessage();

        // Act
        TelemetryMessage copy = TelemetryJson.deserialize(TelemetryJson.serialize(message));

        // Assert
        Assert.Equal(message.DeviceId, copy.DeviceId);
        Assert.Equal(message.SimTimeUtc, copy.SimTimeUtc);
        Assert.Equal(message.PolledAtUtc, copy.PolledAtUtc);
        Assert.Equal(message.Sequence, copy.Sequence);
        Assert.Equal(TelemetryQuality.Good, copy.Quality);
        Assert.Single(copy.Measurements);
        Assert.Equal(3120, copy.Measurements[0].Value);
    }

    [Fact]
    public void Serialize_BadMessageWithoutSimTime_WritesNull()
    {
        // Arrange
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = "pv-1";
        message.Quality = TelemetryQuality.Bad;
        message.Error = "cannot reach 127.0.0.1:1511";

        // Act
        string json = Encoding.UTF8.GetString(TelemetryJson.serialize(message));

        // Assert
        Assert.Contains("\"simTimeUtc\":null", json);
        Assert.Contains("\"quality\":\"Bad\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("not json")]
    [InlineData("{\"quality\":\"Maybe\"}")]
    public void Deserialize_InvalidBody_ThrowsJsonException(string body)
    {
        // Act
        Action deserialize = delegate { TelemetryJson.deserialize(Encoding.UTF8.GetBytes(body)); };

        // Assert
        Assert.ThrowsAny<JsonException>(deserialize);
    }
}
