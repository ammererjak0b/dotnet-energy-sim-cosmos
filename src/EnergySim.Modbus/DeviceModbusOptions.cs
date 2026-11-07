using System.Net;
using EnergySim.Core.Devices;

namespace EnergySim.Modbus;

/// <summary>Bind settings of the device Modbus server.</summary>
public sealed class DeviceModbusOptions
{
    public const string DEFAULT_HOST = "0.0.0.0"; // all interfaces

    public string Host { get; set; } = DEFAULT_HOST;
    public int Port { get; set; }
    public byte UnitId { get; set; } = 1;
    public DeviceType DeviceType { get; set; } = DeviceType.Pv; // selects the device block profile

    public void validate()
    {
        IPAddress? address;
        if (!IPAddress.TryParse(Host, out address))
        {
            throw new ArgumentException("Host must be an IP address.", nameof(Host));
        }

        if (Port < PortRules.MIN_PORT || Port > PortRules.MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), "Port must be " + PortRules.MIN_PORT + " to " + PortRules.MAX_PORT + ".");
        }

        if (UnitId < PortRules.MIN_UNIT_ID || UnitId > PortRules.MAX_UNIT_ID)
        {
            throw new ArgumentOutOfRangeException(nameof(UnitId));
        }

        if (!Enum.IsDefined(typeof(DeviceType), DeviceType))
        {
            throw new ArgumentOutOfRangeException(nameof(DeviceType));
        }
    }
}
