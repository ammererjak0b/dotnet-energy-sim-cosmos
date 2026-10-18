using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

public class TerminalModeTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void IsInteractive_DependsOnRedirection(bool outputRedirected, bool inputRedirected, bool expected)
    {
        // Arrange / Act
        bool result = TerminalMode.isInteractive(outputRedirected, inputRedirected);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsInteractive_UnderTestRunner_IsFalse()
    {
        // Arrange / Act
        bool result = TerminalMode.isInteractive();

        // Assert
        Assert.False(result); // test runner redirects output
    }
}
