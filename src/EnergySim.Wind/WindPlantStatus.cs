namespace EnergySim.Wind;

/// <summary>Operating state of a wind plant, including weather availability and storm protection.</summary>
public enum WindPlantStatus
{
    Off = 0,
    Standby = 1,
    Running = 2,
    Limited = 3,
    Fault = 4,
    StormShutdown = 5,
    WeatherUnavailable = 6
}
