using LB_01.Domain.Entities;
using LB_01.Domain.Snapshots;

namespace LB_01.Domain.Simulation;

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
        updateCharging(charger.port1, timeStep);
        updateCharging(charger.port2, timeStep);

        ChargePortSnapshot[] portSnapshots =
        [
            charger.port1.createSnapshot(
                simulationTime,
                charger.port1.currentPowerKw,
                charger.port1.accumulatedEnergyKwh),
            charger.port2.createSnapshot(
                simulationTime,
                charger.port2.currentPowerKw,
                charger.port2.accumulatedEnergyKwh)
        ];

        double totalPowerKw = portSnapshots.Sum(port => port.currentPowerKw);
        double accumulatedEnergyKwh = portSnapshots.Sum(port => port.accumulatedEnergyKwh);

        return new SimulationSnapshot(
            simulationTime,
            portSnapshots,
            totalPowerKw,
            accumulatedEnergyKwh);
    }

    private static void updateCharging(ChargePort port, TimeSpan timeStep)
    {
        ECar? car = port.connectedCar;

        if (car is null || car.stateOfChargePercent >= FULL_SOC_PERCENT)
        {
            port.currentPowerKw = NO_CHARGING_POWER_KW;
            return;
        }

        double maximumChargingPowerKw = Math.Min(port.maximumPowerKw, car.maximumChargingPowerKw);
        double chargingPowerKw = calculateChargingPower(car.stateOfChargePercent, maximumChargingPowerKw);
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
