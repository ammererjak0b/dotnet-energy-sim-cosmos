using System.Globalization;
using LB_01.Domain.Snapshots;

namespace LB_01.Application;

public static class CsvExporter
{
    private const string HEADER = "simulationTime,port1PowerKw,port1EnergyKwh,port1SocPercent,port2PowerKw,port2EnergyKwh,port2SocPercent,totalPowerKw,totalEnergyKwh";

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
                format(port1.currentPowerKw),
                format(port1.accumulatedEnergyKwh),
                port1SocPercent,
                format(port2.currentPowerKw),
                format(port2.accumulatedEnergyKwh),
                port2SocPercent,
                format(snapshot.totalPowerKw),
                format(snapshot.accumulatedEnergyKwh)));
        }
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
