using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

public class LogBufferTests
{
    [Fact]
    public void Add_BeyondCapacity_KeepsNewestLinesInOrder()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(3);

        // Act
        buffer.add("a");
        buffer.add("b");
        buffer.add("c");
        buffer.add("d");

        // Assert
        Assert.Equal(new List<string> { "b", "c", "d" }, buffer.getLast(10));
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void GetLast_WithSmallerCount_ReturnsOnlyNewest()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(10);
        buffer.add("a");
        buffer.add("b");
        buffer.add("c");

        // Act
        IReadOnlyList<string> lines = buffer.getLast(2);

        // Assert
        Assert.Equal(new List<string> { "b", "c" }, lines);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_Throws()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogBuffer(0));
    }

    [Fact]
    public void ReadSince_ReturnsOnlyNewLines()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(5);
        buffer.add("a");
        LogSlice first = buffer.readSince(0);
        buffer.add("b");
        buffer.add("c");

        // Act
        LogSlice second = buffer.readSince(first.NextSequence);

        // Assert
        Assert.Equal(new List<string> { "a" }, first.Lines);
        Assert.Equal(new List<string> { "b", "c" }, second.Lines);
        Assert.Equal(3, second.NextSequence);
    }

    [Fact]
    public async Task Add_FromManyThreads_LosesNothingAndStaysWithinCapacity()
    {
        // Arrange
        LogBuffer buffer = new LogBuffer(50);
        List<Task> tasks = new List<Task>();

        // Act
        for (int worker = 0; worker < 8; worker++)
        {
            int workerId = worker;
            tasks.Add(Task.Factory.StartNew(() =>
            {
                for (int index = 0; index < 500; index++)
                {
                    buffer.add("w" + workerId + "-" + index);
                    buffer.getLast(5); // concurrent reads
                }
            }, TaskCreationOptions.LongRunning));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(50, buffer.Count);
        Assert.Equal(4000, buffer.TotalAdded);
    }
}
