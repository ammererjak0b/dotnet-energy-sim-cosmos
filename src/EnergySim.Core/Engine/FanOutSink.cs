namespace EnergySim.Core.Engine;

/// <summary>Gives every frame to all sinks (dashboard, CSV). A failing sink does not starve the others.</summary>
public sealed class FanOutSink : ISimulationSink
{
    private readonly List<ISimulationSink> _sinks;

    public FanOutSink(IReadOnlyList<ISimulationSink> sinks)
    {
        if (sinks == null)
        {
            throw new ArgumentNullException(nameof(sinks));
        }

        _sinks = new List<ISimulationSink>(sinks);
    }

    /// <summary>Throws AggregateException with all sink errors after every sink was tried.</summary>
    public async Task publishAsync(SimulationFrame frame, CancellationToken ct)
    {
        List<Exception> errors = new List<Exception>();
        foreach (ISimulationSink sink in _sinks)
        {
            try
            {
                await sink.publishAsync(frame, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }
}
