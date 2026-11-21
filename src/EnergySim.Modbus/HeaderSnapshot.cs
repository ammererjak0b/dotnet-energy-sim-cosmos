using EnergySim.Core.Devices;

namespace EnergySim.Modbus;

/// <summary>Decoded common header (17 input registers) plus the raw values it came from.</summary>
public sealed class HeaderSnapshot
{
    private const ulong MAX_UNIX_MS = 253402300799999UL; // 9999-12-31, DateTimeOffset limit

    private HeaderSnapshot(ushort[] raw)
    {
        Raw = raw;
    }

    public ushort[] Raw { get; }

    public DeviceType Type { get; private set; }

    public DeviceState State { get; private set; }

    public ushort FaultCode { get; private set; }

    public ushort WarnFlags { get; private set; }

    public int ActivePowerW { get; private set; }

    public uint EnergyWh { get; private set; }

    public DateTimeOffset VirtualTime { get; private set; }

    public uint Sequence { get; private set; }

    public ushort Generation { get; private set; }

    public ClockStateCode ClockState { get; private set; }

    /// <summary>Throws a fatal HeaderDecodeException for an unknown device type, map version or clock state.</summary>
    public static HeaderSnapshot decode(ushort[] raw)
    {
        if (raw.Length < RegisterMap.HEADER_REGISTER_COUNT)
        {
            throw new HeaderDecodeException("protocol error: header has " + raw.Length + " registers, expected " + RegisterMap.HEADER_REGISTER_COUNT, false);
        }

        ushort deviceType = raw[RegisterMap.IN_DEVICE_TYPE];
        if (!Enum.IsDefined(typeof(DeviceType), (int)deviceType))
        {
            throw new HeaderDecodeException("unknown device type " + deviceType + " (known 1..4), stopping", true);
        }

        ushort mapVersion = raw[RegisterMap.IN_MAP_VERSION];
        if (mapVersion != RegisterMap.MAP_VERSION)
        {
            throw new HeaderDecodeException("unknown register map version " + mapVersion + " (this client knows " + RegisterMap.MAP_VERSION + "), stopping", true);
        }

        ushort clockState = raw[RegisterMap.IN_CLOCK_STATE];
        if (!Enum.IsDefined(typeof(ClockStateCode), (int)clockState))
        {
            throw new HeaderDecodeException("unknown clock state " + clockState + " (known 0..3), stopping", true);
        }

        HeaderSnapshot header = new HeaderSnapshot(raw);
        header.Type = (DeviceType)deviceType;
        header.State = (DeviceState)raw[RegisterMap.IN_STATE]; // out of range prints as number
        header.FaultCode = raw[RegisterMap.IN_FAULT_CODE];
        header.WarnFlags = raw[RegisterMap.IN_WARN_FLAGS];
        header.ActivePowerW = RegisterCodec.readInt32(raw, RegisterMap.IN_ACTIVE_POWER_W);
        header.EnergyWh = RegisterCodec.readUInt32(raw, RegisterMap.IN_ENERGY_WH);
        ulong virtualTimeMs = RegisterCodec.readUInt64(raw, RegisterMap.IN_VIRTUAL_TIME_MS);
        header.VirtualTime = DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Min(virtualTimeMs, MAX_UNIX_MS));
        header.Sequence = RegisterCodec.readUInt32(raw, RegisterMap.IN_SEQUENCE);
        header.Generation = raw[RegisterMap.IN_GENERATION];
        header.ClockState = (ClockStateCode)clockState;
        return header;
    }

    public List<string> warnNames()
    {
        List<string> names = new List<string>();
        if ((WarnFlags & RegisterMap.WARN_NO_MASTER_SYNC) != 0)
        {
            names.Add("NoMasterSync");
        }

        if ((WarnFlags & RegisterMap.WARN_WEATHER_STALE) != 0)
        {
            names.Add("WeatherStale");
        }

        if ((WarnFlags & RegisterMap.WARN_INVALID_WRITE) != 0)
        {
            names.Add("InvalidWrite");
        }

        return names;
    }

    public string warnText()
    {
        List<string> names = warnNames();
        if (names.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", names);
    }

    public bool hasInvalidWrite()
    {
        return (WarnFlags & RegisterMap.WARN_INVALID_WRITE) != 0;
    }
}
