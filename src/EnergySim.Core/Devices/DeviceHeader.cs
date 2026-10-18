namespace EnergySim.Core.Devices;

/// <summary>Common values every device reports. Sign: generation positive, consumption negative.</summary>
public sealed class DeviceHeader
{
    public DeviceState State { get; set; } = DeviceState.Off;

    public ushort FaultCode { get; set; } // 0 = no fault

    public int ActivePowerW { get; set; }

    public long EnergyWh { get; set; } // cumulative, reset on restart

    public bool IsOn { get; set; }

    public double PowerLimitPercent { get; set; } = 100.0; // 0..100
}
