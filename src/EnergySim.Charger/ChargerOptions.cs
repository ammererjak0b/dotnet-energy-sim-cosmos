namespace EnergySim.Charger;

/// <summary>Charger configuration (section "Charger"). Defaults give a demo day with two cars.</summary>
public sealed class ChargerOptions
{
    public const string SECTION_NAME = "Charger";

    public double Port1MaxPowerKw { get; set; } = 11.0;

    public double Port2MaxPowerKw { get; set; } = 11.0;

    public double TotalMaxPowerKw { get; set; } = 22.0;

    public double TaperStartSocPercent { get; set; } = 80.0; // above this SoC the power is reduced linearly to 100 %

    public double MinimumTaperPowerKw { get; set; } = 1.0; // floor of the taper

    public List<CarScheduleEntry> Schedule { get; set; } = createDefaultSchedule();

    private static List<CarScheduleEntry> createDefaultSchedule()
    {
        List<CarScheduleEntry> schedule = new List<CarScheduleEntry>();

        CarScheduleEntry morningCar = new CarScheduleEntry();
        morningCar.Name = "Commuter Car";
        morningCar.CapacityKwh = 60.0;
        morningCar.StartSocPercent = 30.0;
        morningCar.MaxPowerKw = 11.0;
        morningCar.Port = 1;
        morningCar.ArriveHourUtc = 6;
        morningCar.ArriveMinute = 0;
        morningCar.DepartHourUtc = 10;
        morningCar.DepartMinute = 0;
        schedule.Add(morningCar);

        CarScheduleEntry lateMorningCar = new CarScheduleEntry();
        lateMorningCar.Name = "Delivery Van";
        lateMorningCar.CapacityKwh = 75.0;
        lateMorningCar.StartSocPercent = 20.0;
        lateMorningCar.MaxPowerKw = 11.0;
        lateMorningCar.Port = 2;
        lateMorningCar.ArriveHourUtc = 7;
        lateMorningCar.ArriveMinute = 30;
        lateMorningCar.DepartHourUtc = 12;
        lateMorningCar.DepartMinute = 0;
        schedule.Add(lateMorningCar);

        return schedule;
    }
}
