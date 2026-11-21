using EnergySim.Core.Devices;
using EnergySim.Messaging;
using EnergySim.Modbus;

namespace EnergySim.Gateway;

/// <summary>Pure mapping from decoded registers to a telemetry message. No device specific code, units and scales come from DeviceProfiles.</summary>
public static class TelemetryMapper
{
    public static TelemetryMessage map(string deviceId, HeaderSnapshot header, ushort[] deviceBlock, DateTime polledAtUtc)
    {
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = deviceId;
        message.DeviceType = header.Type.ToString();
        message.SimTimeUtc = header.VirtualTime.UtcDateTime;
        message.PolledAtUtc = polledAtUtc;
        message.Sequence = header.Sequence;
        message.Generation = header.Generation;
        message.ClockState = header.ClockState.ToString();
        message.State = header.State.ToString();
        message.FaultCode = header.FaultCode;
        message.WarnFlags = header.warnNames();
        message.Quality = readQuality(header);
        message.Measurements.Add(createMeasurement("ActivePower", header.ActivePowerW, "W"));
        message.Measurements.Add(createMeasurement("Energy", header.EnergyWh, "Wh"));

        int index = 0;
        foreach (DeviceProfileValue profileValue in DeviceProfiles.forType(header.Type))
        {
            int raw = RegisterCodec.readInt32(deviceBlock, index * RegisterMap.SPECIFIC_REGISTERS_PER_VALUE);
            message.Measurements.Add(createMeasurement(profileValue.Name, RegisterCodec.rawToScaled(raw, profileValue.Scale), profileValue.Unit));
            index++;
        }

        return message;
    }

    /// <summary>Message for a failed read or decode: no values, no virtual time.</summary>
    public static TelemetryMessage mapBad(GatewayDeviceConfig device, DateTime polledAtUtc, string error)
    {
        TelemetryMessage message = new TelemetryMessage();
        message.DeviceId = device.Id;
        message.DeviceType = device.Type.ToString();
        message.PolledAtUtc = polledAtUtc;
        message.Quality = TelemetryQuality.Bad;
        message.Error = error;
        return message;
    }

    private static TelemetryQuality readQuality(HeaderSnapshot header)
    {
        bool noMasterSync = (header.WarnFlags & RegisterMap.WARN_NO_MASTER_SYNC) != 0;
        bool weatherStale = (header.WarnFlags & RegisterMap.WARN_WEATHER_STALE) != 0;
        if (noMasterSync || weatherStale || header.ClockState == ClockStateCode.Unsynced)
        {
            return TelemetryQuality.Uncertain;
        }

        return TelemetryQuality.Good;
    }

    private static Measurement createMeasurement(string name, double value, string unit)
    {
        Measurement measurement = new Measurement();
        measurement.Name = name;
        measurement.Value = value;
        measurement.Unit = unit;
        return measurement;
    }
}
