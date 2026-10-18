using LB_01.src.Application;

namespace LB_01.Tests;

public class SimulationControllerConfigurationTests
{
    [Fact]
    public void ConfigurationCommandPausesSimulationAndRequestsConfiguration()
    {
        SimulationController controller = new();

        Assert.True(controller.processCommand('c'));

        Assert.False(controller.isRunning);
        Assert.True(controller.shouldConfigure);
        Assert.False(controller.shouldQuit);
    }

    [Fact]
    public void ConfigurationCanBeCompletedBeforeResuming()
    {
        SimulationController controller = new();

        controller.processCommand('c');
        controller.completeConfiguration();

        Assert.False(controller.shouldConfigure);
        Assert.False(controller.isRunning);
        Assert.True(controller.processCommand('r'));
        Assert.True(controller.isRunning);
    }
}
