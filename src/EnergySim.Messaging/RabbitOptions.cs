using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Messaging;

/// <summary>Broker settings. Host, port, vhost, exchange from configuration, user and password only from environment (RABBITMQ_USER, RABBITMQ_PASSWORD).</summary>
public sealed class RabbitOptions
{
    public const string USER_VARIABLE = "RABBITMQ_USER";
    public const string PASSWORD_VARIABLE = "RABBITMQ_PASSWORD";

    public string Host { get; private set; } = "localhost";

    public int Port { get; private set; } = 5672;

    public string VirtualHost { get; private set; } = "/";

    public string Exchange { get; private set; } = Routing.DEFAULT_EXCHANGE;

    public string User { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    /// <summary>Throws InvalidOperationException for a missing credential or an invalid port.</summary>
    public static RabbitOptions load(IConfiguration configuration)
    {
        RabbitOptions options = new RabbitOptions();
        IConfigurationSection section = configuration.GetSection("Rabbit");
        string? host = section["Host"];
        if (!string.IsNullOrEmpty(host))
        {
            options.Host = host;
        }

        string? virtualHost = section["VirtualHost"];
        if (!string.IsNullOrEmpty(virtualHost))
        {
            options.VirtualHost = virtualHost;
        }

        string? exchange = section["Exchange"];
        if (!string.IsNullOrEmpty(exchange))
        {
            options.Exchange = exchange;
        }

        string? portText = section["Port"];
        if (!string.IsNullOrEmpty(portText))
        {
            int port;
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)
            {
                throw new InvalidOperationException("Rabbit:Port '" + portText + "' is not a port (1..65535)");
            }

            options.Port = port;
        }

        options.User = readVariable(USER_VARIABLE);
        options.Password = readVariable(PASSWORD_VARIABLE);
        return options;
    }

    private static string readVariable(string name)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException("environment variable " + name + " is not set (copy .env.example to .env, see docs/rabbitmq.md)");
        }

        return value;
    }
}
