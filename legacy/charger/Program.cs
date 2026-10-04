using LB_01.Application;
using LB_01.Domain.Entities;
using LB_01.Domain.Simulation;

namespace LB_01
{
    internal class Program
    {
        private const int CHARGER_ID = 1;
        private const string CHARGER_NAME = "Demo Charger";
        private const double PORT_MAXIMUM_POWER_KW = 11;
        private const double MAXIMUM_TOTAL_POWER_KW = 22;
        private const int CAR_ID = 1;
        private const double BATTERY_CAPACITY_KWH = 60;
        private const double INITIAL_STATE_OF_CHARGE_PERCENT = 60;
        private const double MAXIMUM_CHARGING_POWER_KW = 11;
        private const int SIMULATION_STEP_MINUTES = 15;
        private const int DEMO_STEP_COUNT = 100;
        private const int DISPLAY_DELAY_MILLISECONDS = 500;

        static void Main(string[] args)
        {
            DualPortCharger charger = new(
                CHARGER_ID,
                CHARGER_NAME,
                PORT_MAXIMUM_POWER_KW,
                PORT_MAXIMUM_POWER_KW,
                MAXIMUM_TOTAL_POWER_KW);
            ECar car = new()
            {
                id = CAR_ID,
                name = "Demo Car",
                manufacturer = "Demo Manufacturer",
                model = "Demo Model",
                batteryCapacityKwh = BATTERY_CAPACITY_KWH,
                stateOfChargePercent = INITIAL_STATE_OF_CHARGE_PERCENT,
                maximumChargingPowerKw = MAXIMUM_CHARGING_POWER_KW
            };
            charger.tryConnect(charger.port1, car);

            Simulation simulation = new(charger);
            TimeSpan simulationStep = TimeSpan.FromMinutes(SIMULATION_STEP_MINUTES);

            for (int step = 0; step < DEMO_STEP_COUNT; step++)
            {
                Console.Clear();
                Console.Write(ConsoleDashboard.render(simulation.advance(simulationStep)));
                Thread.Sleep(DISPLAY_DELAY_MILLISECONDS);
            }
        }
    }
}
