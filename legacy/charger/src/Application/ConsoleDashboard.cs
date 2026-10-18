using System.Globalization;
using System.Text;
using LB_01.src.Domain.Snapshots;

namespace LB_01.src.Application;

public static class ConsoleDashboard
{
    private const int SOC_BAR_LENGTH = 10;

    public static string render(SimulationSnapshot snapshot)
    {
        StringBuilder output = new();
        output.AppendLine("=== Dual-Port Charger Simulation ===");
        output.AppendLine($"Charger status: {snapshot.chargerStatus.ToString().ToUpperInvariant()}");
        if (snapshot.chargerFault is not null)
        {
            output.AppendLine($"Charger fault: {snapshot.chargerFault}");
        }

        output.AppendLine($"Simulation time: {snapshot.simulationTime:hh\\:mm\\:ss}");
        output.AppendLine($"Total power: {snapshot.totalPowerKw.ToString("F2", CultureInfo.InvariantCulture)} kW");
        output.AppendLine($"Accumulated energy: {snapshot.accumulatedEnergyKwh.ToString("F2", CultureInfo.InvariantCulture)} kWh");

        foreach (ChargePortSnapshot port in snapshot.ports)
        {
            output.AppendLine();
            output.AppendLine($"Port {port.portNumber}: {port.status.ToString().ToUpperInvariant()} | {port.currentPowerKw.ToString("F2", CultureInfo.InvariantCulture)} / {port.maximumPowerKw.ToString("F2", CultureInfo.InvariantCulture)} kW");

            if (port.fault is not null)
            {
                output.AppendLine($"  Fault: {port.fault}");
            }

            if (port.car is null)
            {
                output.AppendLine("  Available");
                continue;
            }

            string stateOfCharge = port.car.stateOfChargePercent.ToString("F1", CultureInfo.InvariantCulture);
            output.AppendLine($"  Car: {port.car.name}");
            output.AppendLine($"  SOC: {stateOfCharge}% {createSocBar(port.car.stateOfChargePercent)}");
        }

        return output.ToString();
    }

    private static string createSocBar(double stateOfChargePercent)
    {
        int filledLength = (int)Math.Round(
            Math.Clamp(stateOfChargePercent, 0, 100) / 100 * SOC_BAR_LENGTH);
        return $"[{new string('#', filledLength)}{new string('-', SOC_BAR_LENGTH - filledLength)}]";
    }
}
