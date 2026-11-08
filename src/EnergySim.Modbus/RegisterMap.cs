namespace EnergySim.Modbus;

/// <summary>
/// Register addresses of the device Modbus server (client side, 0-based). Contract: docs/contracts/modbus-registers.md.
/// Input registers (FC04) are read-only, holding registers (FC03 read, FC06 write).
/// </summary>
public static class RegisterMap
{
    public const int MAP_VERSION = 1;

    // input registers, common header
    public const int IN_DEVICE_TYPE = 0; // u16, DeviceType 1..4
    public const int IN_MAP_VERSION = 1; // u16
    public const int IN_STATE = 2; // u16, DeviceState 0..4
    public const int IN_FAULT_CODE = 3; // u16, 0 = ok
    public const int IN_WARN_FLAGS = 4; // u16, bit flags
    public const int IN_ACTIVE_POWER_W = 5; // s32, 2 registers, generation positive
    public const int IN_ENERGY_WH = 7; // u32, 2 registers
    public const int IN_VIRTUAL_TIME_MS = 9; // u64, 4 registers, unix ms UTC
    public const int IN_SEQUENCE = 13; // u32, 2 registers
    public const int IN_GENERATION = 15; // u16
    public const int IN_CLOCK_STATE = 16; // u16, see ClockStateCode
    public const int HEADER_REGISTER_COUNT = 17; // read the whole header in one request

    // input registers, device block: value i at SPECIFIC_START + 2 * i, s32 scaled
    public const int SPECIFIC_START = 100;
    public const int SPECIFIC_REGISTERS_PER_VALUE = 2;

    // holding registers, common controls
    public const int HOLD_ON_OFF = 0; // u16, OFF..ON
    public const int HOLD_LIMIT_PERMILLE = 1; // u16, 0..1000, 0.1 % steps
    public const int HOLD_FAULT_RESET = 2; // u16, write 1, server sets back to 0
    public const int HOLD_REGISTER_COUNT = 3;

    public const ushort ON = 1; // HOLD_ON_OFF values
    public const ushort OFF = 0;

    // warn flags (IN_WARN_FLAGS)
    public const ushort WARN_NO_MASTER_SYNC = 1; // bit 0
    public const ushort WARN_WEATHER_STALE = 2; // bit 1
    public const ushort WARN_INVALID_WRITE = 4; // bit 2, cleared by the next valid write

    public const int LIMIT_PERMILLE_MAX = 1000;
}
