namespace EnergySim.Modbus;

/// <summary>Immutable copy of one Modbus holding-register write.</summary>
public sealed class ClockRegisterWrite
{
    public int StartingAddress { get; }
    public IReadOnlyList<int> Values { get; }

    public ClockRegisterWrite(int startingAddress, IReadOnlyList<int> values)
    {
        StartingAddress = startingAddress;
        Values = new List<int>(values);
    }
}
