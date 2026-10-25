using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Time;

/// <summary>Scheduled state and protocol operation delivered to devices.</summary>
public sealed class ClockTransition
{
    public ClockDefinition Definition { get; }
    public ClockMessageKind Kind { get; }

    public ClockTransition(ClockDefinition definition, ClockMessageKind kind)
    {
        Definition = definition.copy();
        Kind = kind;
    }
}
