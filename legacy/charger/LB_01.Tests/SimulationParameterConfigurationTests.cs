using LB_01.src.Application;

namespace LB_01.Tests;

public class SimulationParameterConfigurationTests
{
    private const int INITIAL_SIMULATION_STEP_MINUTES = 15;
    private const int INITIAL_DISPLAY_DELAY_MILLISECONDS = 500;
    private const int UPDATED_SIMULATION_STEP_MINUTES = 5;
    private const int UPDATED_DISPLAY_DELAY_MILLISECONDS = 100;

    [Fact]
    public void MenuUpdatesSelectedParametersAndKeepsUnselectedParameter()
    {
        SimulationParameterConfiguration configuration = new(
            INITIAL_SIMULATION_STEP_MINUTES,
            INITIAL_DISPLAY_DELAY_MILLISECONDS);
        using StringReader input = new("1\n5\n2\n100\n0\n");
        using StringWriter output = new();

        configuration.configure(input, output);

        Assert.Equal(UPDATED_SIMULATION_STEP_MINUTES, configuration.simulationStepMinutes);
        Assert.Equal(UPDATED_DISPLAY_DELAY_MILLISECONDS, configuration.displayDelayMilliseconds);
    }
}
