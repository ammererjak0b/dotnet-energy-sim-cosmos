using EnergySim.Messaging;
using Microsoft.Extensions.Configuration;

namespace EnergySim.UnitTests.Messaging;

/// <summary>Broker settings: host, port from configuration, credentials only from the environment.</summary>
[Collection(RabbitEnvironmentCollection.NAME)]
public sealed class RabbitOptionsTests : IDisposable
{
    private readonly string? _oldUser = Environment.GetEnvironmentVariable(RabbitOptions.USER_VARIABLE);
    private readonly string? _oldPassword = Environment.GetEnvironmentVariable(RabbitOptions.PASSWORD_VARIABLE);

    public RabbitOptionsTests()
    {
        Environment.SetEnvironmentVariable(RabbitOptions.USER_VARIABLE, "tester");
        Environment.SetEnvironmentVariable(RabbitOptions.PASSWORD_VARIABLE, "secret");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(RabbitOptions.USER_VARIABLE, _oldUser);
        Environment.SetEnvironmentVariable(RabbitOptions.PASSWORD_VARIABLE, _oldPassword);
    }

    private static IConfiguration createConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Load_NoSettings_UsesDefaultsAndEnvironmentCredentials()
    {
        // Act
        RabbitOptions options = RabbitOptions.load(createConfiguration(new Dictionary<string, string?>()));

        // Assert
        Assert.Equal("localhost", options.Host);
        Assert.Equal(5672, options.Port);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal(Routing.DEFAULT_EXCHANGE, options.Exchange);
        Assert.Equal("tester", options.User);
        Assert.Equal("secret", options.Password);
    }

    [Fact]
    public void Load_HostAndPortFromConfiguration_AreUsed()
    {
        // Arrange
        Dictionary<string, string?> values = new Dictionary<string, string?>();
        values["Rabbit:Host"] = "rabbitmq";
        values["Rabbit:Port"] = "5673";

        // Act
        RabbitOptions options = RabbitOptions.load(createConfiguration(values));

        // Assert
        Assert.Equal("rabbitmq", options.Host);
        Assert.Equal(5673, options.Port);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("70000")]
    [InlineData("abc")]
    public void Load_InvalidPort_Throws(string port)
    {
        // Arrange
        Dictionary<string, string?> values = new Dictionary<string, string?>();
        values["Rabbit:Port"] = port;

        // Act
        Action load = delegate { RabbitOptions.load(createConfiguration(values)); };

        // Assert
        Assert.Throws<InvalidOperationException>(load);
    }

    [Theory]
    [InlineData(RabbitOptions.USER_VARIABLE)]
    [InlineData(RabbitOptions.PASSWORD_VARIABLE)]
    public void Load_MissingCredential_ThrowsAndNamesTheVariable(string variable)
    {
        // Arrange
        Environment.SetEnvironmentVariable(variable, null);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(delegate { RabbitOptions.load(createConfiguration(new Dictionary<string, string?>())); });

        // Assert
        Assert.Contains(variable, exception.Message);
    }
}
