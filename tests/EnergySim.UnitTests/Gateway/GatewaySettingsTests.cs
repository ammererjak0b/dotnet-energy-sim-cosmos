using EnergySim.Core.Devices;
using EnergySim.Gateway;
using EnergySim.UnitTests.Messaging;
using Microsoft.Extensions.Configuration;

namespace EnergySim.UnitTests.Gateway;

/// <summary>Gateway configuration: defaults, devices, and every invalid value gives a clear error.</summary>
[Collection(RabbitEnvironmentCollection.NAME)]
public sealed class GatewaySettingsTests : IDisposable
{
    private readonly string? _oldUser = Environment.GetEnvironmentVariable("RABBITMQ_USER");
    private readonly string? _oldPassword = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD");

    public GatewaySettingsTests()
    {
        Environment.SetEnvironmentVariable("RABBITMQ_USER", "tester");
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", "secret");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("RABBITMQ_USER", _oldUser);
        Environment.SetEnvironmentVariable("RABBITMQ_PASSWORD", _oldPassword);
    }

    private static Dictionary<string, string?> createValidValues()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>();
        values["Devices:0:Id"] = "pv-1";
        values["Devices:0:Type"] = "Pv";
        values["Devices:0:Host"] = "127.0.0.1";
        values["Devices:0:Port"] = "1511";
        values["Devices:1:Id"] = "wind-1";
        values["Devices:1:Type"] = "wind";
        values["Devices:1:Host"] = "127.0.0.1";
        values["Devices:1:Port"] = "1514";
        return values;
    }

    private static GatewaySettings load(Dictionary<string, string?> values)
    {
        return GatewaySettings.load(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    [Fact]
    public void Load_ValidDevices_UsesDefaultsForPollAndUnit()
    {
        // Act
        GatewaySettings settings = load(createValidValues());

        // Assert
        Assert.Equal(1000, settings.PollIntervalMs);
        Assert.Equal(1, settings.UnitId);
        Assert.Equal(2, settings.Devices.Count);
        Assert.Equal("pv-1", settings.Devices[0].Id);
        Assert.Equal(DeviceType.Pv, settings.Devices[0].Type);
        Assert.Equal(1511, settings.Devices[0].Port);
        Assert.Equal(DeviceType.Wind, settings.Devices[1].Type); // type name is case insensitive
    }

    [Fact]
    public void Load_PollIntervalAndUnitFromConfiguration_AreUsed()
    {
        // Arrange
        Dictionary<string, string?> values = createValidValues();
        values["PollIntervalMs"] = "500";
        values["UnitId"] = "5";

        // Act
        GatewaySettings settings = load(values);

        // Assert
        Assert.Equal(500, settings.PollIntervalMs);
        Assert.Equal(5, settings.UnitId);
    }

    [Theory]
    [InlineData("PollIntervalMs", "99")]
    [InlineData("PollIntervalMs", "60001")]
    [InlineData("PollIntervalMs", "fast")]
    [InlineData("UnitId", "0")]
    [InlineData("UnitId", "248")]
    [InlineData("Devices:0:Type", "Toaster")]
    [InlineData("Devices:0:Port", "1501")]
    [InlineData("Devices:0:Port", "0")]
    [InlineData("Devices:0:Host", "")]
    [InlineData("Devices:1:Id", "pv-1")]
    public void Load_InvalidValue_ThrowsInvalidOperation(string key, string value)
    {
        // Arrange
        Dictionary<string, string?> values = createValidValues();
        values[key] = value;

        // Act
        Action act = delegate { load(values); };

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Load_NoDevices_Throws()
    {
        // Act
        Action act = delegate { load(new Dictionary<string, string?>()); };

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Load_DuplicateDeviceId_NamesTheId()
    {
        // Arrange
        Dictionary<string, string?> values = createValidValues();
        values["Devices:1:Id"] = "pv-1";

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(delegate { load(values); });

        // Assert
        Assert.Contains("pv-1", exception.Message);
    }
}
