using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

public class FanOutSinkTests
{
    [Fact]
    public async Task PublishAsync_SendsFrameToAllSinks()
    {
        // Arrange
        FakeSimulationSink first = new FakeSimulationSink();
        FakeSimulationSink second = new FakeSimulationSink();
        FanOutSink fanOut = new FanOutSink(new List<ISimulationSink> { first, second });
        SimulationFrame frame = new SimulationFrame();

        // Act
        await fanOut.publishAsync(frame, CancellationToken.None);

        // Assert
        Assert.Same(frame, first.LastFrame);
        Assert.Same(frame, second.LastFrame);
    }

    [Fact]
    public async Task PublishAsync_FirstSinkThrows_SecondStillGetsFrame()
    {
        // Arrange
        FakeSimulationSink broken = new FakeSimulationSink();
        broken.ThrowOnPublish = true;
        FakeSimulationSink healthy = new FakeSimulationSink();
        FanOutSink fanOut = new FanOutSink(new List<ISimulationSink> { broken, healthy });

        // Act
        await Assert.ThrowsAsync<AggregateException>(delegate { return fanOut.publishAsync(new SimulationFrame(), CancellationToken.None); });

        // Assert
        Assert.Equal(1, healthy.FrameCount);
    }
}
