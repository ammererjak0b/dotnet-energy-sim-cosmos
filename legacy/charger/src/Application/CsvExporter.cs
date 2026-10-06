using System.Globalization;
using LB_01.src.Domain.Snapshots;

namespace LB_01.src.Application;

public static class CsvExporter
{
    private const string HEADER = "simulationTime,chargerStatus,chargerFault,port1Status,port1Fault,port1CarId,port1CarName,port1PowerKw,port1EnergyKwh,port1SocPercent,port2Status,port2Fault,port2CarId,port2CarName,port2PowerKw,port2EnergyKwh,port2SocPercent,totalPowerKw,totalEnergyKwh";

    public static void write(
        IEnumerable<SimulationSnapshot> snapshots,
        TextWriter output)
    {
        output.WriteLine(HEADER);

        foreach (SimulationSnapshot snapshot in snapshots)
        {
            ChargePortSnapshot port1 = snapshot.ports[0];
            ChargePortSnapshot port2 = snapshot.ports[1];
            string port1SocPercent = getSocPercent(port1);
            string port2SocPercent = getSocPercent(port2);

            output.WriteLine(string.Join(",",
                snapshot.simulationTime.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture),
                snapshot.chargerStatus,
                snapshot.chargerFault ?? string.Empty,
                port1.status,
                port1.fault ?? string.Empty,
                getCarId(port1),
                escapeCsv(getCarName(port1)),
                format(port1.currentPowerKw),
                format(port1.accumulatedEnergyKwh),
                port1SocPercent,
                port2.status,
                port2.fault ?? string.Empty,
                getCarId(port2),
                escapeCsv(getCarName(port2)),
                format(port2.currentPowerKw),
                format(port2.accumulatedEnergyKwh),
                port2SocPercent,
                format(snapshot.totalPowerKw),
                format(snapshot.accumulatedEnergyKwh)));
        }
    }

    private static string getCarId(ChargePortSnapshot port)
    {
        return port.car is null
            ? string.Empty
            : port.car.id.ToString(CultureInfo.InvariantCulture);
    }

    private static string getCarName(ChargePortSnapshot port)
    {
        return port.car?.name ?? string.Empty;
    }

    private static string escapeCsv(string value)
    {
        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static string getSocPercent(ChargePortSnapshot port)
    {
        return port.car is null
            ? string.Empty
            : format(port.car.stateOfChargePercent);
    }

    private static string format(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
