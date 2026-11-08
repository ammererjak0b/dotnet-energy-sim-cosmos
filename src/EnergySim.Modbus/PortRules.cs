namespace EnergySim.Modbus;

/// <summary>One definition of the allowed Modbus ports and unit ids for all projects.</summary>
public static class PortRules
{
    public const int MIN_PORT = 1502; // 1502 itself is the time service, device ports above
    public const int MAX_PORT = 65535;
    public const int MIN_UNIT_ID = 1;
    public const int MAX_UNIT_ID = 247;
}
