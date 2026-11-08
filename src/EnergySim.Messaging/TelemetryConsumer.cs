using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EnergySim.Messaging;

/// <summary>Receives telemetry through an own exclusive queue bound to one routing pattern. Live view, no backlog.</summary>
public sealed class TelemetryConsumer : IAsyncDisposable
{
    private readonly RabbitOptions _options;
    private readonly string _bindingKey;
    private readonly Func<TelemetryMessage, Task> _onMessage;
    private readonly Func<string, Task> _onInvalidMessage;
    private IConnection? _connection;
    private IChannel? _channel;
    private TaskCompletionSource _closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public TelemetryConsumer(RabbitOptions options, string bindingKey, Func<TelemetryMessage, Task> onMessage, Func<string, Task> onInvalidMessage)
    {
        _options = options;
        _bindingKey = bindingKey;
        _onMessage = onMessage;
        _onInvalidMessage = onInvalidMessage;
    }

    /// <summary>Completes when the connection is lost or closed.</summary>
    public Task Closed
    {
        get
        {
            return _closed.Task;
        }
    }

    /// <summary>Connects, declares exchange and queue, binds and starts receiving. Throws BrokerUnavailableException when the broker is unreachable.</summary>
    public async Task connectAsync(CancellationToken cancellationToken)
    {
        await closeAsync();
        _closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            ConnectionFactory factory = RabbitConnection.createFactory(_options);
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _connection.ConnectionShutdownAsync += onShutdownAsync;
            _channel = await _connection.CreateChannelAsync(null, cancellationToken);
            _channel.ChannelShutdownAsync += onShutdownAsync; // channel closed (queue deleted) also means reconnect
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, true, false, null, false, false, cancellationToken);
            QueueDeclareOk queue = await _channel.QueueDeclareAsync(string.Empty, false, true, true, null, false, false, cancellationToken); // server named, exclusive, auto-delete
            await _channel.QueueBindAsync(queue.QueueName, _options.Exchange, _bindingKey, null, false, cancellationToken);
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += onReceivedAsync;
            await _channel.BasicConsumeAsync(queue.QueueName, true, consumer, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await closeAsync();
            throw new BrokerUnavailableException("cannot connect to " + _options.Host + ":" + _options.Port + " (" + exception.Message + ")", exception);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await closeAsync();
    }

    private async Task onReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        TelemetryMessage message;
        try
        {
            message = TelemetryJson.deserialize(eventArgs.Body);
        }
        catch (JsonException exception)
        {
            await _onInvalidMessage(eventArgs.RoutingKey + ": " + exception.Message);
            return;
        }

        await _onMessage(message);
    }

    private Task onShutdownAsync(object? sender, ShutdownEventArgs eventArgs)
    {
        _closed.TrySetResult();
        return Task.CompletedTask;
    }

    private async Task closeAsync()
    {
        try
        {
            if (_connection != null)
            {
                _connection.ConnectionShutdownAsync -= onShutdownAsync;
            }

            if (_channel != null)
            {
                _channel.ChannelShutdownAsync -= onShutdownAsync;
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
