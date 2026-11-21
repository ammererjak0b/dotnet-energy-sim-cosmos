namespace EnergySim.Messaging;

/// <summary>Broker cannot be reached or the connection broke. Hides the RabbitMQ.Client exception types from Gateway and Viewer.</summary>
public sealed class BrokerUnavailableException : Exception
{
    public BrokerUnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
