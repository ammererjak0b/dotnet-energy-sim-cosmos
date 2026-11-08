using EnergySim.Core.Engine;

namespace EnergySim.Modbus;

/// <summary>Copies every published frame into the Modbus registers. The loop publishes every tick, also while paused.</summary>
public sealed class DeviceModbusSink : ISimulationSink
{
    private readonly DeviceModbusServer _server;

    public DeviceModbusSink(DeviceModbusServer server)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        _server = server;
    }

    public Task publishAsync(SimulationFrame frame, CancellationToken ct)
    {
        _server.updateFrame(frame); // synchronous memory copy, no I/O
        return Task.CompletedTask;
    }
}
