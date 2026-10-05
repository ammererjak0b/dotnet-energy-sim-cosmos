using System.Globalization;

namespace LB_01.Application;

public sealed class SimulationParameterConfiguration
{
    public SimulationParameterConfiguration(int simulationStepMinutes, int displayDelayMilliseconds)
    {
        this.simulationStepMinutes = simulationStepMinutes;
        this.displayDelayMilliseconds = displayDelayMilliseconds;
    }

    public int simulationStepMinutes { get; private set; }

    public int displayDelayMilliseconds { get; private set; }

    public void configure(TextReader input, TextWriter output)
    {
        bool isFinished = false;

        while (!isFinished)
        {
            output.WriteLine();
            output.WriteLine("Simulation parameters:");
            output.WriteLine($"1. Simulation step: {simulationStepMinutes} minutes");
            output.WriteLine($"2. Display delay: {displayDelayMilliseconds} milliseconds");
            output.WriteLine("0. Finish configuration");
            output.Write("Select parameter: ");
            string? selection = input.ReadLine();

            switch (selection)
            {
                case "1":
                    simulationStepMinutes = readPositiveInteger(
                        input,
                        output,
                        "Simulation step in minutes",
                        simulationStepMinutes);
                    break;
                case "2":
                    displayDelayMilliseconds = readPositiveInteger(
                        input,
                        output,
                        "Display delay in milliseconds",
                        displayDelayMilliseconds);
                    break;
                case "0":
                    isFinished = true;
                    break;
                default:
                    output.WriteLine("Please select 1, 2 or 0.");
                    break;
            }
        }
    }

    private static int readPositiveInteger(
        TextReader input,
        TextWriter output,
        string label,
        int currentValue)
    {
        while (true)
        {
            output.Write($"{label} [{currentValue}]: ");
            string? value = input.ReadLine();

            if (string.IsNullOrWhiteSpace(value))
            {
                return currentValue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                && result > 0)
            {
                return result;
            }

            output.WriteLine("Please enter a positive whole number.");
        }
    }
}
