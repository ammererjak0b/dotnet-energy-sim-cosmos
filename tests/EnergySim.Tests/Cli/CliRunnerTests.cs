using EnergySim.Cli;

namespace EnergySim.Tests.Cli;

/// <summary>Checks the public CLI help without starting a simulator or reading weather files.</summary>
[Collection("Console output")]
public class CliRunnerTests
{
    /// <summary>Lists every selectable device, including wind.</summary>
    [Fact]
    public async Task RunAsync_WithHelp_ListsAllDeviceNames()
    {
        // Arrange
        TextWriter previousOutput = Console.Out;
        using StringWriter output = new StringWriter();
        Console.SetOut(output);
        int exitCode;

        // Act
        try
        {
            exitCode = await CliRunner.runAsync(new string[] { "--help" }, CancellationToken.None);
        }
        finally
        {
            Console.SetOut(previousOutput);
        }

        // Assert
        Assert.Equal(0, exitCode);
        foreach (string deviceName in CliSettings.DEVICE_NAMES)
        {
            Assert.Contains(deviceName, output.ToString());
        }
    }
}
