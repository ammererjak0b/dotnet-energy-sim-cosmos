using LB_01.src.Application;

namespace LB_01.Tests;

public class SimulationControllerTests
{
    private const int FIRST_STEP = 1;
    private const int SECOND_STEP = 2;
    private const int THIRD_STEP = 3;

    [Fact]
    public void PauseAndResumeCommandsControlAutomaticSimulation()
    {
        SimulationController controller = new();

        Assert.True(controller.isRunning);
        Assert.True(controller.processCommand('p'));
        Assert.False(controller.isRunning);
        Assert.False(controller.shouldAdvance);
        Assert.True(controller.processCommand('r'));
        Assert.True(controller.isRunning);
        Assert.True(controller.shouldAdvance);
        Assert.True(controller.processCommand('q'));
        Assert.True(controller.shouldQuit);
        Assert.False(controller.isRunning);
    }

    [Fact]
    public void AutomaticSimulationAdvancesOnlyWhileRunning()
    {
        SimulationController controller = new();

        Assert.Equal(FIRST_STEP, controller.getNextStep());
        Assert.Equal(SECOND_STEP, controller.getNextStep());
        controller.processCommand('p');
        Assert.Equal(THIRD_STEP, controller.getNextStep());
        controller.processCommand('r');
        Assert.Equal(THIRD_STEP, controller.getNextStep());
    }
}
