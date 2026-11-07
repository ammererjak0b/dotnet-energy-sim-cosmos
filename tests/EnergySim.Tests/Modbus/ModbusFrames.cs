using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Tests.Modbus;

/// <summary>Hand-built SimulationFrame for the Modbus server tests.</summary>
internal static class ModbusFrames
{
    public static readonly DateTimeOffset VIRTUAL_NOW = new DateTimeOffset(2026, 6, 21, 12, 30, 15, TimeSpan.Zero);

    public static SimulationFrame create(DeviceType type, double[] specificValues)
    {
        IReadOnlyList<DeviceProfileValue> profile = DeviceProfiles.forType(type);
        List<DeviceValue> values = new List<DeviceValue>();
        for (int index = 0; index < profile.Count; index++)
        {
            values.Add(new DeviceValue(profile[index].Name, specificValues[index]));
        }

        SimulationFrame frame = new SimulationFrame();
        frame.VirtualNow = VIRTUAL_NOW;
        frame.Sequence = 70000;
        frame.SpecificValues = values;
        frame.Snapshot.Generation = 3;
        frame.Snapshot.Status = ClockStatus.Running;
        frame.Snapshot.IsSynced = true;
        frame.Header.State = DeviceState.Running;
        frame.Header.ActivePowerW = -1500;
        frame.Header.EnergyWh = 123456;
        frame.Header.IsOn = true;
        frame.Header.PowerLimitPercent = 100.0;
        return frame;
    }

    public static SimulationFrame createPv()
    {
        return create(DeviceType.Pv, new double[] { 800.0, 45.5, 32.1, 1234.0 });
    }
}
