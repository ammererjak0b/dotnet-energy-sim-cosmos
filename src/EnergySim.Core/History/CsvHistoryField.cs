namespace EnergySim.Core.History;

/// <summary>A named value captured from one device state for CSV output.</summary>
public sealed class CsvHistoryField
{
    public CsvHistoryField(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}
