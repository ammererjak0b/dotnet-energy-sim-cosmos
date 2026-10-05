using LB_01.Application;
using LB_01.Domain.Simulation;

namespace LB_01;

internal class Program
{
    private const int SIMULATION_STEP_MINUTES = 15;
    private const int DISPLAY_DELAY_MILLISECONDS = 1000;

    static void Main(string[] args)
    {
        bool shouldQuit = false;

        while (!shouldQuit)
        {
            SimulationConfiguration configuration = SimulationConfigurationDialog.readConfiguration(
                Console.In,
                Console.Out);
            Simulation simulation = new(configuration.createCharger());
            SimulationController controller = new();
            SimulationParameterConfiguration parameters = new(
                SIMULATION_STEP_MINUTES,
                DISPLAY_DELAY_MILLISECONDS);

            Console.WriteLine("Simulation started. Press P to pause, R to resume, X to restart, Q to quit.");

            while (!controller.shouldQuit && !controller.shouldRestart)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    controller.processCommand(key.KeyChar);
                }

                if (controller.shouldConfigure)
                {
                    parameters.configure(Console.In, Console.Out);
                    controller.completeConfiguration();
                }

                if (controller.shouldAdvance)
                {
                    TimeSpan simulationStep = TimeSpan.FromMinutes(parameters.simulationStepMinutes);
                    Console.Clear();
                    Console.Write(ConsoleDashboard.render(simulation.advance(simulationStep)));
                    Console.WriteLine();
                    Console.WriteLine($"Step: {parameters.simulationStepMinutes} min | Delay: {parameters.displayDelayMilliseconds} ms");
                    Console.WriteLine("P = pause | R = resume | C = configure | X = restart | Q = quit");
                }
                else
                {
                    int cursorLeft = Console.CursorLeft;
                    int cursorTop = Console.CursorTop;
                    Console.SetCursorPosition(0, cursorTop);
                    Console.Write("Simulation paused. Press R to resume, X to restart or Q to quit. ");
                    Console.SetCursorPosition(cursorLeft, cursorTop);
                }

                Thread.Sleep(parameters.displayDelayMilliseconds);
            }

            shouldQuit = controller.shouldQuit;
        }
    }
}