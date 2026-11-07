using EnergySim.Core.Engine;
using System.Threading.Channels;

namespace EnergySim.Modbus;

/// <summary>Registers a device and applies service definitions and heartbeats to its local clock.</summary>
public sealed class SharedClockConnection
{
    private readonly SharedClockConnectionOptions _connectionOptions;
    private readonly ClockEndpointOptions _endpointOptions;
    private readonly ClockEndpointServer _endpoint;
    private readonly ScheduledClock _clock;
    private readonly TimeProvider _timeProvider;
    private readonly Action<string> _reportStatus;
    private bool _serviceUnavailableReported;

    public IClockSource Clock
    {
        get
        {
            return _clock;
        }
    }

    public SharedClockConnection(
        SharedClockConnectionOptions connectionOptions,
        ClockEndpointOptions endpointOptions,
        TimeProvider timeProvider,
        Action<string> reportStatus)
    {
        connectionOptions.validate();
        endpointOptions.validate();
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(reportStatus);
        _connectionOptions = connectionOptions;
        _endpointOptions = endpointOptions;
        _timeProvider = timeProvider;
        _reportStatus = reportStatus;
        _clock = new ScheduledClock(timeProvider);
        _endpoint = new ClockEndpointServer(endpointOptions);
    }

    public async Task runAsync(CancellationToken cancellationToken)
    {
        _endpoint.start();
        Task definitionTask = processDefinitionsAsync(cancellationToken);
        Task heartbeatTask = processHeartbeatsAsync(cancellationToken);
        try
        {
            TimeSpan interval = TimeSpan.FromMilliseconds(_connectionOptions.RegistrationRetryMilliseconds);
            using PeriodicTimer timer = new PeriodicTimer(interval, _timeProvider);
            Task registrationTask = registerAndMonitorAsync(timer, cancellationToken);
            await Task.WhenAll(registrationTask, definitionTask, heartbeatTask);
        }
        finally
        {
            await _endpoint.DisposeAsync();
        }
    }

    private async Task registerAndMonitorAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        while (true)
        {
            if (!_clock.getSnapshot().IsSynced)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using ClockModbusClient client = new ClockModbusClient(
                        _connectionOptions.ServiceHost,
                        _connectionOptions.ServicePort,
                        _connectionOptions.UnitIdentifier);
                    client.connect();
                    int[] serviceRegisters = client.readInputRegisters(
                        ClockServiceInfoCodec.REGISTER_ADDRESS,
                        ClockServiceInfoCodec.REGISTER_COUNT);
                    ClockServiceInfo serviceInfo = ClockServiceInfoCodec.decode(serviceRegisters);
                    _clock.HeartbeatExpiry = TimeSpan.FromSeconds(serviceInfo.HeartbeatExpirySeconds);
                    client.writeSingleRegister(0, _endpointOptions.Port);
                }
                catch (Exception exception)
                {
                    if (!_serviceUnavailableReported)
                    {
                        _reportStatus("Time service unavailable: " + exception.Message);
                        _serviceUnavailableReported = true;
                    }
                }

            }
            else if (_serviceUnavailableReported)
            {
                _reportStatus("Time service connection restored.");
                _serviceUnavailableReported = false;
            }

            await timer.WaitForNextTickAsync(cancellationToken);
        }
    }

    private async Task processDefinitionsAsync(CancellationToken cancellationToken)
    {
        await foreach ((ClockDefinition definition, ClockMessageKind kind) message in _endpoint.Definitions.ReadAllAsync(cancellationToken))
        {
            if (message.kind == ClockMessageKind.Initialize)
            {
                _clock.initialize(message.definition, true);
            }
            else
            {
                _clock.receive(message.definition, message.kind == ClockMessageKind.Restart);
            }

            ClockSnapshot snapshot = _clock.getSnapshot();
            string statusMessage = snapshot.SyncError;
            if (statusMessage.Length == 0)
            {
                statusMessage = "Clock revision " + snapshot.Revision + " received.";
            }

            _reportStatus(statusMessage);
        }
    }

    private async Task processHeartbeatsAsync(CancellationToken cancellationToken)
    {
        await foreach (ClockHeartbeat heartbeat in _endpoint.Heartbeats.ReadAllAsync(cancellationToken))
        {
            _clock.heartbeat(heartbeat.SessionId, heartbeat.Revision, heartbeat.IssuedTimestamp);
        }
    }
}
