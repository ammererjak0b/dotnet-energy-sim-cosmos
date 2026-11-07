using System.Buffers.Binary;
using EnergySim.Core.Engine;
using System.Threading.Channels;

namespace EnergySim.Modbus;

/// <summary>Device-side clock endpoint; receives definitions and heartbeats while telemetry is separate.</summary>
public sealed class ClockEndpointServer : IAsyncDisposable
{
    public const int DEFINITION_ADDRESS = 0;
    public const int HEARTBEAT_ADDRESS = 32;
    public const int IDENTITY_ADDRESS = 0;
    public const int IDENTITY_REGISTER_COUNT = 18;

    private readonly ClockModbusServer _server;
    private readonly ClockEndpointOptions _options;
    private readonly Channel<(ClockDefinition definition, ClockMessageKind kind)> _definitions =
        Channel.CreateUnbounded<(ClockDefinition definition, ClockMessageKind kind)>();
    private readonly Channel<ClockHeartbeat> _heartbeats = Channel.CreateUnbounded<ClockHeartbeat>();
    private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
    private Task? _writeTask;

    public ChannelReader<(ClockDefinition definition, ClockMessageKind kind)> Definitions
    {
        get
        {
            return _definitions.Reader;
        }
    }

    public ChannelReader<ClockHeartbeat> Heartbeats
    {
        get
        {
            return _heartbeats.Reader;
        }
    }

    public ClockEndpointServer(ClockEndpointOptions options)
    {
        options.validate();
        _options = options;
        _server = new ClockModbusServer(options);
        writeIdentity();
    }

    public void start()
    {
        _writeTask = processWritesAsync(_shutdown.Token);
        _server.start();
    }

    public async ValueTask DisposeAsync()
    {
        _definitions.Writer.TryComplete();
        _heartbeats.Writer.TryComplete();
        _shutdown.Cancel();
        _server.Dispose();
        if (_writeTask != null)
        {
            await _writeTask;
            _writeTask = null;
        }

        _shutdown.Dispose();
    }

    private void writeIdentity()
    {
        int[] values = ClockIdentityCodec.encode(_options.DeviceId);
        for (int index = 0; index < values.Length; index++)
        {
            _server.setInputRegister(IDENTITY_ADDRESS + index, values[index]);
        }
    }

    private async Task processWritesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (ClockRegisterWrite write in _server.Writes.ReadAllAsync(cancellationToken))
            {
                try
                {
                    if (write.StartingAddress == DEFINITION_ADDRESS && write.Values.Count == ClockCodec.REGISTER_COUNT)
                    {
                        ClockDefinition definition = ClockCodec.decode(toArray(write.Values), out ClockMessageKind kind);
                        _definitions.Writer.TryWrite((definition, kind));
                    }
                    else if (write.StartingAddress == HEARTBEAT_ADDRESS && write.Values.Count == HeartbeatCodec.REGISTER_COUNT)
                    {
                        _heartbeats.Writer.TryWrite(HeartbeatCodec.decode(toArray(write.Values)));
                    }
                }
                catch (ArgumentException)
                {
                    // Invalid messages are discarded.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal endpoint shutdown.
        }
    }

    private static int[] toArray(IReadOnlyList<int> values)
    {
        int[] copy = new int[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return copy;
    }
}
