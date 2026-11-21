using RabbitMQ.Client;

namespace EnergySim.Messaging;

/// <summary>Publishes telemetry to the topic exchange. No automatic recovery: the caller sees a broken connection and reconnects visibly.</summary>
public sealed class TelemetryPublisher : IAsyncDisposable
{
    private readonly RabbitOptions _options;
    private IConnection? _connection;
    private IChannel? _channel;

    public TelemetryPublisher(RabbitOptions options)
    {
        _options = options;
    }

    public bool IsOpen
    {
        get
        {
            if (_connection == null || _channel == null)
            {
                return false;
            }

            return _connection.IsOpen && _channel.IsOpen;
        }
    }

    /// <summary>Connects and declares the exchange. Throws BrokerUnavailableException when the broker is unreachable.</summary>
    public async Task connectAsync(CancellationToken cancellationToken)
    {
        await closeAsync();
        try
        {
            ConnectionFactory factory = RabbitConnection.createFactory(_options);
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(null, cancellationToken);
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, true, false, null, false, false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await closeAsync();
            throw new BrokerUnavailableException("cannot connect to " + _options.Host + ":" + _options.Port + " (" + exception.Message + ")", exception);
        }
    }

    public async Task publishAsync(TelemetryMessage message, CancellationToken cancellationToken)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("publisher is not connected");
        }

        BasicProperties properties = new BasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = DeliveryModes.Transient; // telemetry, latest value matters
        string routingKey = Routing.routingKey(message.DeviceType, message.DeviceId);
        try
        {
            await _channel.BasicPublishAsync(_options.Exchange, routingKey, false, properties, TelemetryJson.serialize(message), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new BrokerUnavailableException("publish failed (" + exception.Message + ")", exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await closeAsync();
    }

    private async Task closeAsync()
    {
        try
        {
            if (_channel != null)
            {
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // already broken, nothing left to close
        }

        _channel = null;
        _connection = null;
    }
}
