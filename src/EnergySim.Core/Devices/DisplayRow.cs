namespace EnergySim.Core.Devices;

/// <summary>One extra line in the shared device dashboard.</summary>
public sealed class DisplayRow
{
    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty; // already formatted incl. unit

    public DisplaySeverity Severity { get; set; } = DisplaySeverity.Normal;

    public bool HasBar { get; set; }

    public double BarFraction { get; set; } // 0..1, only used when HasBar
}
