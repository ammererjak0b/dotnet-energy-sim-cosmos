namespace EnergySim.Core.Engine;

/// <summary>Receives one frame per tick (dashboard, CSV history).</summary>
public interface ISimulationSink
{
    Task publishAsync(SimulationFrame frame, CancellationToken ct);
}
