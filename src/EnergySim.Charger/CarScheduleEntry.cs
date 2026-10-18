namespace EnergySim.Charger;

/// <summary>One car that arrives and leaves at fixed virtual times of day (UTC), every day.</summary>
public sealed class CarScheduleEntry
{
    public string Name { get; set; } = "Scheduled Car";

    public double CapacityKwh { get; set; } = 60.0;

    public double StartSocPercent { get; set; } = 30.0; // SoC on arrival

    public double MaxPowerKw { get; set; } = 11.0; // car side charging limit

    public int Port { get; set; } = 1; // 1 or 2

    public int ArriveHourUtc { get; set; }

    public int ArriveMinute { get; set; }

    public int DepartHourUtc { get; set; }

    public int DepartMinute { get; set; }
}
