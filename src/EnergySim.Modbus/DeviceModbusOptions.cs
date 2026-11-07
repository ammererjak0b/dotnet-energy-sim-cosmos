using EnergySim.Core.Devices;

namespace EnergySim.Modbus;

/// <summary>Bind settings and identity of the device Modbus server.</summary>
public sealed class DeviceModbusOptions
{
    public const int MIN_PORT = 1502; // 1502 or higher, 1502 itself is the time service
    public const int MAX_PORT = 65535;
    public const int MAX_UNIT_ID = 247;

    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; }
    public byte UnitId { get; set; } = 1;
    public string DeviceId { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.Pv; // selects the device block profile

    public void validate()
    {
        System.Net.IPAddress? address;
        if (!System.Net.IPAddress.TryParse(Host, out address))
        {
            throw new ArgumentException("Host must be an IP address.", nameof(Host));
        }

        if (Port < MIN_PORT || Port > MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), "Port must be 1502 or higher.");
        }

        if (UnitId < 1 || UnitId > MAX_UNIT_ID)
        {
            throw new ArgumentOutOfRangeException(nameof(UnitId));
        }

        if (DeviceId.Length == 0)
        {
            throw new ArgumentException("DeviceId must not be empty.", nameof(DeviceId));
        }

        if (!Enum.IsDefined(typeof(DeviceType), DeviceType))
        {
            throw new ArgumentOutOfRangeException(nameof(DeviceType));
        }
    }
}
