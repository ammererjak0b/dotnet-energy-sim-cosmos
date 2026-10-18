using EnergySim.Core.Devices;
using EnergySim.Terminal;
using Spectre.Console;

namespace EnergySim.Tests.Terminal;

public class TerminalBlocksTests
{
    [Fact]
    public void StatusHeader_ShowsTitleLinesAndBanner()
    {
        // Arrange
        List<string> lines = new List<string> { "Time 06:00", "Speed 60x" };

        // Act
        string text = RenderHelper.render(TerminalBlocks.statusHeader("PV pv-1", lines, "NO MASTER", DisplaySeverity.Warning));

        // Assert
        Assert.Contains("PV pv-1", text);
        Assert.Contains("Time 06:00", text);
        Assert.Contains("Speed 60x", text);
        Assert.Contains("NO MASTER", text);
    }

    [Fact]
    public void StatusHeader_WithEmptyBanner_ShowsNoBannerLine()
    {
        // Arrange
        List<string> lines = new List<string> { "Time 06:00" };

        // Act
        string text = RenderHelper.render(TerminalBlocks.statusHeader("PV", lines, string.Empty, DisplaySeverity.Normal));

        // Assert
        Assert.DoesNotContain("NO MASTER", text);
        Assert.Contains("Time 06:00", text);
    }

    [Fact]
    public void KeyValueTable_ShowsLabelsValuesAndBar()
    {
        // Arrange
        DisplayRow plain = new DisplayRow { Label = "Power", Value = "1.2 kW" };
        DisplayRow withBar = new DisplayRow { Label = "SoC", Value = "50 %", HasBar = true, BarFraction = 0.5 };
        List<DisplayRow> rows = new List<DisplayRow> { plain, withBar };

        // Act
        string text = RenderHelper.render(TerminalBlocks.keyValueTable(rows));

        // Assert
        Assert.Contains("Power", text);
        Assert.Contains("1.2 kW", text);
        Assert.Contains("SoC", text);
        Assert.Contains("50 %", text);
        Assert.Contains("█████░░░░░", text); // 10 wide bar
    }

    [Theory]
    [InlineData(0.0, "░░░░░░░░░░")]
    [InlineData(0.5, "█████░░░░░")]
    [InlineData(1.0, "██████████")]
    [InlineData(2.0, "██████████")] // clamped
    [InlineData(-1.0, "░░░░░░░░░░")] // clamped
    public void KeyValueTable_Bar_FillsProportionally(double fraction, string expectedBar)
    {
        // Arrange
        DisplayRow row = new DisplayRow();
        row.Label = "Load";
        row.Value = "x";
        row.HasBar = true;
        row.BarFraction = fraction;

        // Act
        string text = RenderHelper.render(TerminalBlocks.keyValueTable(new List<DisplayRow> { row }));

        // Assert
        Assert.Contains(expectedBar, text);
    }

    [Fact]
    public void LogPanel_ShowsOnlyLastFiveLines()
    {
        // Arrange
        List<string> lines = new List<string>();
        for (int index = 1; index <= 8; index++)
        {
            lines.Add("line" + index);
        }

        // Act
        string text = RenderHelper.render(TerminalBlocks.logPanel(lines));

        // Assert
        Assert.DoesNotContain("line3", text);
        Assert.Contains("line4", text);
        Assert.Contains("line8", text);
    }

    [Fact]
    public void LogPanel_ShowsMarkupCharactersLiterally()
    {
        // Arrange
        List<string> lines = new List<string> { "[red]raw[/]" };

        // Act
        string text = RenderHelper.render(TerminalBlocks.logPanel(lines));

        // Assert
        Assert.Contains("[red]raw[/]", text);
    }

    [Fact]
    public void KeyLegend_ListsCommandsAndBuiltInQuit()
    {
        // Arrange
        List<CommandDescriptor> commands = new List<CommandDescriptor>
        {
            new CommandDescriptor { Name = CommonCommands.TURN_ON, Key = 'O', Label = "On" },
            new CommandDescriptor { Name = CommonCommands.SET_LIMIT, Key = 'L', Label = "Limit" }
        };

        // Act
        string text = RenderHelper.render(TerminalBlocks.keyLegend(commands));

        // Assert
        Assert.Contains("[O] On", text);
        Assert.Contains("[L] Limit", text);
        Assert.Contains("[Q] Quit", text);
    }

    [Fact]
    public void Style_MapsSeverityToTrafficLightColors()
    {
        // Arrange / Act / Assert
        Assert.Equal(Color.Green, TerminalStyle.colorFor(DisplaySeverity.Normal));
        Assert.Equal(Color.Yellow, TerminalStyle.colorFor(DisplaySeverity.Warning));
        Assert.Equal(Color.Red, TerminalStyle.colorFor(DisplaySeverity.Fault));
    }
}
