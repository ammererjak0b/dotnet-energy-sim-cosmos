using LB_01.src.Domain.Entities;
using LB_01.src.Domain.Snapshots;

namespace LB_01.src.Domain.Simulation;

public sealed class Simulation
{
    private const double NO_CHARGING_POWER_KW = 0;
    private const double FULL_POWER_SOC_LIMIT_PERCENT = 80;
    private const double FULL_SOC_PERCENT = 100;
    private const double MINIMUM_TAPER_POWER_KW = 1;
    private readonly DualPortCharger charger;

    public Simulation(DualPortCharger charger)
    {
        this.charger = charger;
    }

    public TimeSpan simulationTime { get; private set; }

    public SimulationSnapshot advance(TimeSpan timeStep)
    {
        simulationTime += timeStep;
        Dictionary<ChargePort, double> chargingPowersKw = calculateChargingPowers();
        updateCharging(charger.port1, chargingPowersKw[charger.port1], timeStep);
        updateCharging(charger.port2, chargingPowersKw[charger.port2], timeStep);

        ChargePortSnapshot[] portSnapshots =
        [
            charger.port1.createSnapshot(
                simulationTime,
                charger.port1.currentPowerKw,
                charger.port1.accumulatedEnergyKwh,
                getPortStatus(charger.port1),
                charger.port1.fault),
            charger.port2.createSnapshot(
                simulationTime,
                charger.port2.currentPowerKw,
                charger.port2.accumulatedEnergyKwh,
                getPortStatus(charger.port2),
                charger.port2.fault)
        ];

        double totalPowerKw = portSnapshots.Sum(port => port.currentPowerKw);
        double accumulatedEnergyKwh = portSnapshots.Sum(port => port.accumulatedEnergyKwh);

        return new SimulationSnapshot(
            simulationTime,
            portSnapshots,
            totalPowerKw,
            accumulatedEnergyKwh,
            charger.status,
            charger.fault);
    }

    private ChargePortStatus getPortStatus(ChargePort port)
    {
        if (charger.status != ChargerStatus.On)
        {
            return ChargePortStatus.Off;
        }

        return port.getStatus();
    }

    private Dictionary<ChargePort, double> calculateChargingPowers()
    {
        Dictionary<ChargePort, double> requestedPowersKw = new()
        {
            [charger.port1] = calculateRequestedChargingPower(charger.port1),
            [charger.port2] = calculateRequestedChargingPower(charger.port2)
        };
        Dictionary<ChargePort, double> assignedPowersKw = requestedPowersKw.ToDictionary(
            entry => entry.Key,
            entry => 0d);
        List<ChargePort> activePorts = requestedPowersKw
            .Where(entry => entry.Value > NO_CHARGING_POWER_KW)
            .Select(entry => entry.Key)
            .ToList();
        double remainingPowerKw = charger.status == ChargerStatus.On
            ? charger.maximumTotalPowerKw
            : NO_CHARGING_POWER_KW;

        while (activePorts.Count > 0 && remainingPowerKw > NO_CHARGING_POWER_KW)
        {
            double fairShareKw = remainingPowerKw / activePorts.Count;
            List<ChargePort> portsReachingRequest = [];

            foreach (ChargePort port in activePorts)
            {
                double remainingRequestKw = requestedPowersKw[port] - assignedPowersKw[port];
                double assignedPowerKw = Math.Min(remainingRequestKw, fairShareKw);
                assignedPowersKw[port] += assignedPowerKw;
                remainingPowerKw -= assignedPowerKw;

                if (assignedPowersKw[port] >= requestedPowersKw[port])
                {
                    portsReachingRequest.Add(port);
                }
            }

            foreach (ChargePort port in portsReachingRequest)
            {
                activePorts.Remove(port);
            }
        }

        return assignedPowersKw;
    }

    private static double calculateRequestedChargingPower(ChargePort port)
    {
        ECar? car = port.connectedCar;

        if (car is null || car.stateOfChargePercent >= FULL_SOC_PERCENT || port.hasFault)
        {
            return NO_CHARGING_POWER_KW;
        }

        double maximumChargingPowerKw = Math.Min(port.maximumPowerKw, car.maximumChargingPowerKw);
        return calculateChargingPower(car.stateOfChargePercent, maximumChargingPowerKw);
    }

    private static void updateCharging(ChargePort port, double chargingPowerKw, TimeSpan timeStep)
    {
        ECar? car = port.connectedCar;

        if (car is null || car.stateOfChargePercent >= FULL_SOC_PERCENT)
        {
            port.currentPowerKw = NO_CHARGING_POWER_KW;
            return;
        }

        double remainingEnergyToFullKwh = car.batteryCapacityKwh
            * (1 - car.stateOfChargePercent / 100);
        double requestedEnergyKwh = chargingPowerKw * timeStep.TotalHours;
        double chargedEnergyKwh = Math.Min(requestedEnergyKwh, remainingEnergyToFullKwh);
        bool reachesFullCharge = remainingEnergyToFullKwh <= requestedEnergyKwh;

        port.currentPowerKw = !reachesFullCharge && timeStep.TotalHours > 0
            ? chargedEnergyKwh / timeStep.TotalHours
            : NO_CHARGING_POWER_KW;
        port.accumulatedEnergyKwh += chargedEnergyKwh;
        car.stateOfChargePercent = reachesFullCharge
            ? FULL_SOC_PERCENT
            : car.stateOfChargePercent + chargedEnergyKwh / car.batteryCapacityKwh * 100;
    }

    private static double calculateChargingPower(
        double stateOfChargePercent,
        double maximumChargingPowerKw)
    {
        if (stateOfChargePercent <= FULL_POWER_SOC_LIMIT_PERCENT)
        {
            return maximumChargingPowerKw;
        }

        double taperFactor = (FULL_SOC_PERCENT - stateOfChargePercent)
            / (FULL_SOC_PERCENT - FULL_POWER_SOC_LIMIT_PERCENT);

        return Math.Max(
            MINIMUM_TAPER_POWER_KW,
            maximumChargingPowerKw * taperFactor);
    }
}
