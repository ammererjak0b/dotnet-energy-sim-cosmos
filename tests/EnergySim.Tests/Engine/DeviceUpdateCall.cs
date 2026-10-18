using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Engine;

public sealed class DeviceUpdateCall
{
    public DeviceUpdateCall(DateTimeOffset virtualNow, TimeSpan step, WeatherDto? weather)
    {
        VirtualNow = virtualNow;
        Step = step;
        Weather = weather;
    }

    public DateTimeOffset VirtualNow { get; }

    public TimeSpan Step { get; }

    public WeatherDto? Weather { get; }
}
