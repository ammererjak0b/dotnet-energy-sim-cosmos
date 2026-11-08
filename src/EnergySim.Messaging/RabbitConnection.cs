using RabbitMQ.Client;

namespace EnergySim.Messaging;

internal static class RabbitConnection
{
    private const int CONNECT_TIMEOUT_SECONDS = 5;

    public static ConnectionFactory createFactory(RabbitOptions options)
    {
        ConnectionFactory factory = new ConnectionFactory();
        factory.HostName = options.Host;
        factory.Port = options.Port;
        factory.VirtualHost = options.VirtualHost;
        factory.UserName = options.User;
        factory.Password = options.Password;
        factory.AutomaticRecoveryEnabled = false; // own visible reconnect loop
        factory.RequestedConnectionTimeout = TimeSpan.FromSeconds(CONNECT_TIMEOUT_SECONDS);
        return factory;
    }
}
