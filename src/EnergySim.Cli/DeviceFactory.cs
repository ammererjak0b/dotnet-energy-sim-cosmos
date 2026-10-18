using EnergySim.Pv;
using EnergySim.Battery;
using EnergySim.Charger;
using EnergySim.Core.Devices;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Cli;

/// <summary>Builds the chosen device with its options from appsettings.json sections Pv, Battery, Charger (class defaults when absent).</summary>
public static class DeviceFactory
{
    private const string SCHEDULE_KEY = "Schedule";

    public static ISimulatedDevice create(CliSettings settings, IConfiguration configuration)
    {
        try
        {
            if (settings.Device == "pv")
            {
                return new PvDevice(settings.Id, bindPvOptions(configuration));
            }

            if (settings.Device == "battery")
            {
                return new BatteryDevice(settings.Id, bindBatteryOptions(configuration));
            }

            if (settings.Device == "charger")
            {
                return new ChargerDevice(settings.Id, bindChargerOptions(configuration));
            }
        }
        catch (ArgumentException exception)
        {
            throw new CliUsageException("Invalid settings for " + settings.Device + " (section " + sectionName(settings.Device) + "): " + exception.Message);
        }

        throw new CliUsageException("device: unknown '" + settings.Device + "'");
    }

    public static PvOptions bindPvOptions(IConfiguration configuration)
    {
        PvOptions options = new PvOptions();
        configuration.GetSection(PvOptions.SECTION_NAME).Bind(options);
        return options;
    }

    public static BatteryOptions bindBatteryOptions(IConfiguration configuration)
    {
        BatteryOptions options = new BatteryOptions();
        configuration.GetSection(BatteryOptions.SECTION_NAME).Bind(options);
        return options;
    }

    /// <summary>A schedule in the config replaces the default schedule (the binder would append to it).</summary>
    public static ChargerOptions bindChargerOptions(IConfiguration configuration)
    {
        ChargerOptions options = new ChargerOptions();
        IConfigurationSection section = configuration.GetSection(ChargerOptions.SECTION_NAME);
        if (section.GetSection(SCHEDULE_KEY).Exists())
        {
            options.Schedule = new List<CarScheduleEntry>();
        }

        section.Bind(options);
        return options;
    }

    private static string sectionName(string device)
    {
        if (device == "pv")
        {
            return PvOptions.SECTION_NAME;
        }

        if (device == "battery")
        {
            return BatteryOptions.SECTION_NAME;
        }

        return ChargerOptions.SECTION_NAME;
    }
}
