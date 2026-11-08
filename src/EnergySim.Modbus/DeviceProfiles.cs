using EnergySim.Core.Devices;

namespace EnergySim.Modbus;

/// <summary>Device-specific value tables, one per device type. A device must report its SpecificValues in exactly this order.</summary>
public static class DeviceProfiles
{
    public static IReadOnlyList<DeviceProfileValue> forType(DeviceType type)
    {
        List<DeviceProfileValue> values = new List<DeviceProfileValue>();
        if (type == DeviceType.Pv)
        {
            values.Add(new DeviceProfileValue("Irradiance", "W/m2", 1));
            values.Add(new DeviceProfileValue("ModuleTemperature", "degC", 10));
            values.Add(new DeviceProfileValue("SunElevation", "deg", 10));
            values.Add(new DeviceProfileValue("DailyEnergy", "Wh", 1));
        }

        if (type == DeviceType.Battery)
        {
            values.Add(new DeviceProfileValue("StateOfCharge", "%", 10));
            values.Add(new DeviceProfileValue("CellTemperature", "degC", 10));
            values.Add(new DeviceProfileValue("ChargedEnergy", "Wh", 1));
            values.Add(new DeviceProfileValue("DischargedEnergy", "Wh", 1));
        }

        if (type == DeviceType.Charger)
        {
            values.Add(new DeviceProfileValue("Port1Power", "W", 1));
            values.Add(new DeviceProfileValue("Port2Power", "W", 1));
            values.Add(new DeviceProfileValue("Port1VehicleSoc", "%", 10));
            values.Add(new DeviceProfileValue("Port2VehicleSoc", "%", 10));
        }

        if (type == DeviceType.Wind)
        {
            values.Add(new DeviceProfileValue("WindSpeed", "m/s", 10));
            values.Add(new DeviceProfileValue("RotorSpeed", "rpm", 10));
            values.Add(new DeviceProfileValue("NacelleDirection", "deg", 10));
        }

        return values;
    }
}
