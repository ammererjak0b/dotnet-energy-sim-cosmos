using LB_01.Application;
using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Tests;

public class CsvExporterTests
{
    private const int PORT_NUMBER = 1;
    private const double MAXIMUM_POWER_KW = 11;
    private const double CURRENT_POWER_KW = 5.5;
    private const double ACCUMULATED_ENERGY_KWH = 1.375;
    private const double STATE_OF_CHARGE_PERCENT = 60.916666;
    private const int CAR_ID = 1;
    private const double BATTERY_CAPACITY_KWH = 60;
    private const double MAXIMUM_CHARGING_POWER_KW = 11;
    private const double TOTAL_POWER_KW = 5.5;
    private const double TOTAL_ENERGY_KWH = 1.375;

    [Fact]
    public void WritesHeaderAndSnapshotValues()
    {
        ECarSnapshot car = new(
            CAR_ID,
            "Car 1",
            "Manufacturer",
            "Model",
            BATTERY_CAPACITY_KWH,
            STATE_OF_CHARGE_PERCENT,
            MAXIMUM_CHARGING_POWER_KW);
        ChargePortSnapshot port = new(
            TimeSpan.FromMinutes(15),
            PORT_NUMBER,
            MAXIMUM_POWER_KW,
            CURRENT_POWER_KW,
            ACCUMULATED_ENERGY_KWH,
            car);
        ChargePortSnapshot emptyPort = new(
            TimeSpan.FromMinutes(15),
            2,
            MAXIMUM_POWER_KW,
            0,
            0,
            null);
        SimulationSnapshot snapshot = new(
            TimeSpan.FromMinutes(15),
            [port, emptyPort],
            TOTAL_POWER_KW,
            TOTAL_ENERGY_KWH);
        using StringWriter output = new();

        CsvExporter.write([snapshot], output);

        string[] lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("simulationTime,port1PowerKw,port1EnergyKwh,port1SocPercent,port2PowerKw,port2EnergyKwh,port2SocPercent,totalPowerKw,totalEnergyKwh", lines[0]);
        Assert.Contains("00:15:00", lines[1]);
        Assert.Contains("5.50", lines[1]);
        Assert.Contains("60.92", lines[1]);
        Assert.EndsWith("5.50,1.38", lines[1]);
    }
}
