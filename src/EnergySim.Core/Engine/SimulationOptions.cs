namespace EnergySim.Core.Engine;

public sealed class SimulationOptions
{
    public const int DEFAULT_TICK_MILLISECONDS = 200;

    public const int DEFAULT_MAX_STEP_SECONDS = 86400; // one virtual day

    public int TickMilliseconds { get; set; } = DEFAULT_TICK_MILLISECONDS; // real time

    public string LocationId { get; set; } = string.Empty; // weather location, required when device needs weather

    public int MaxStepSeconds { get; set; } = DEFAULT_MAX_STEP_SECONDS; // virtual, larger steps are clamped
}
