using EnergySim.Core.Devices;
using EnergySim.Charger;

namespace EnergySim.Tests.Charger;

/// <summary>Shared helpers for the charger tests.</summary>
public static class ChargerTestSupport
{
    public const string DEVICE_ID = "chg-1";

    public static readonly DateTimeOffset MidnightUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset utcTime(int hour, int minute)
    {
        return MidnightUtc.AddHours(hour).AddMinutes(minute);
    }

    /// <summary>Options without schedule: tests connect cars themselves.</summary>
    public static ChargerOptions createOptions()
    {
        ChargerOptions options = new ChargerOptions();
        options.Schedule = new List<CarScheduleEntry>();
        return options;
    }

    public static ChargerDevice createDevice()
    {
        return new ChargerDevice(DEVICE_ID, createOptions());
    }

    public static ChargerDevice createDevice(ChargerOptions options)
    {
        return new ChargerDevice(DEVICE_ID, options);
    }

    public static ECar createCar(double stateOfChargePercent)
    {
        return createCar(stateOfChargePercent, 11);
    }

    public static ECar createCar(double stateOfChargePercent, double maxPowerKw)
    {
        ECar car = new ECar();
        car.Name = "Test Car";
        car.CapacityKwh = 60;
        car.StateOfChargePercent = stateOfChargePercent;
        car.MaxPowerKw = maxPowerKw;
        return car;
    }

    public static CarScheduleEntry createEntry(int port, int arriveHour, int arriveMinute, int departHour, int departMinute)
    {
        CarScheduleEntry entry = new CarScheduleEntry();
        entry.Name = "Scheduled Car";
        entry.CapacityKwh = 60;
        entry.StartSocPercent = 30;
        entry.MaxPowerKw = 11;
        entry.Port = port;
        entry.ArriveHourUtc = arriveHour;
        entry.ArriveMinute = arriveMinute;
        entry.DepartHourUtc = departHour;
        entry.DepartMinute = departMinute;
        return entry;
    }

    /// <summary>Port values by name, e.g. Port2SocPercent. Name = "Port" + number + Power W | SocPercent | Status.</summary>
    public static double getValue(ChargerDevice device, string name)
    {
        int portNumber = int.Parse(name.Substring(4, 1));
        string quantity = name.Substring(5);
        if (quantity == "PowerW")
        {
            return device.getPortPowerKw(portNumber) * 1000.0;
        }

        if (quantity == "SocPercent")
        {
            return device.getPortSocPercent(portNumber);
        }

        if (quantity == "Status")
        {
            return device.getPortStatus(portNumber);
        }

        throw new InvalidOperationException("Unknown value: " + name);
    }

    public static void advance(ChargerDevice device, TimeSpan step)
    {
        device.update(MidnightUtc.Add(step), step, null);
    }
}
