using EnergySim.Core.Weather;
using EnergySim.Core.History;

namespace EnergySim.Core.Devices;

/// <summary>
/// The only thing a new simulator has to implement. Pure device physics: no console, no I/O, no clock access.
/// Sign convention for power: generation positive, consumption negative.
/// </summary>
public interface ISimulatedDevice
{
    string DeviceId { get; }

    DeviceType Type { get; }

    bool NeedsWeather { get; }

    DeviceHeader Header { get; }

    IReadOnlyList<DisplayRow> DisplayRows { get; }

    IReadOnlyList<CsvHistoryField> CsvHistoryFields { get; }

    IReadOnlyList<CommandDescriptor> Commands { get; }

    /// <summary>Advance the model by step (virtual time). weather is null when NeedsWeather is false or unavailable.</summary>
    void update(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather);

    /// <summary>Back to the initial state.</summary>
    void reset(DateTimeOffset virtualNow);

    CommandResult applyCommand(DeviceCommand command);
}
