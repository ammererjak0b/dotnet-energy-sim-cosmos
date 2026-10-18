using EnergySim.Core.Devices;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace EnergySim.Terminal;

/// <summary>Pure building blocks for every console screen. Text is shown literally (no markup parsing).</summary>
public static class TerminalBlocks
{
    private const int DASHBOARD_BAR_WIDTH = 10;
    private const int LOG_LINES_SHOWN = 5;
    private const char BAR_FILLED = '█';
    private const char BAR_EMPTY = '░';
    private const char QUIT_KEY = 'Q';

    /// <summary>Panel with title, info lines and an optional colored banner (empty text = none).</summary>
    public static IRenderable statusHeader(string title, IReadOnlyList<string> lines, string bannerText, DisplaySeverity bannerSeverity)
    {
        List<IRenderable> content = new List<IRenderable>();
        foreach (string line in lines)
        {
            content.Add(new Text(line));
        }

        Color borderColor = Color.Grey;
        if (bannerText.Length > 0)
        {
            Style bannerStyle = new Style(TerminalStyle.colorFor(bannerSeverity), null, Decoration.Bold);
            content.Add(new Text(bannerText, bannerStyle));
            borderColor = TerminalStyle.colorFor(bannerSeverity);
        }

        Panel panel = new Panel(new Rows(content));
        panel.Header = new PanelHeader(Markup.Escape(title));
        panel.BorderStyle = new Style(borderColor);
        panel.Expand = true;
        return panel;
    }

    /// <summary>Label, colored value and optional bar per row.</summary>
    public static IRenderable keyValueTable(IReadOnlyList<DisplayRow> rows)
    {
        bool anyBar = false;
        foreach (DisplayRow row in rows)
        {
            if (row.HasBar)
            {
                anyBar = true;
            }
        }

        Table table = new Table();
        table.Border = TableBorder.Rounded;
        table.ShowHeaders = false;
        table.AddColumn("Label");
        table.AddColumn("Value");
        if (anyBar)
        {
            table.AddColumn("Bar");
        }

        foreach (DisplayRow row in rows)
        {
            Style style = TerminalStyle.styleFor(row.Severity);
            List<IRenderable> cells = new List<IRenderable>();
            cells.Add(new Text(row.Label));
            cells.Add(new Text(row.Value, style));
            if (anyBar)
            {
                if (row.HasBar)
                {
                    cells.Add(new Text(buildBar(row.BarFraction, DASHBOARD_BAR_WIDTH), style));
                }
                else
                {
                    cells.Add(new Text(string.Empty));
                }
            }

            table.AddRow(cells);
        }

        return table;
    }

    /// <summary>Panel with the newest lines.</summary>
    public static IRenderable logPanel(IReadOnlyList<string> lines)
    {
        List<IRenderable> content = new List<IRenderable>();
        int skip = lines.Count - LOG_LINES_SHOWN;
        for (int index = 0; index < lines.Count; index++)
        {
            if (index >= skip)
            {
                content.Add(new Text(lines[index]));
            }
        }

        if (content.Count == 0)
        {
            content.Add(new Text(string.Empty));
        }

        Panel panel = new Panel(new Rows(content));
        panel.Header = new PanelHeader("Log");
        panel.Expand = true;
        return panel;
    }

    /// <summary>"[O] On  [L] Limit  [Q] Quit". Quit is built in unless a command already uses Q.</summary>
    public static IRenderable keyLegend(IReadOnlyList<CommandDescriptor> commands)
    {
        List<string> entries = new List<string>();
        bool quitDefined = false;
        foreach (CommandDescriptor command in commands)
        {
            char key = char.ToUpperInvariant(command.Key);
            entries.Add("[" + key + "] " + command.Label);
            if (key == QUIT_KEY)
            {
                quitDefined = true;
            }
        }

        if (!quitDefined)
        {
            entries.Add("[" + QUIT_KEY + "] Quit");
        }

        return new Text(string.Join("  ", entries), new Style(Color.Grey));
    }

    private static string buildBar(double fraction, int width)
    {
        double clamped = fraction;
        if (double.IsNaN(clamped) || clamped < 0)
        {
            clamped = 0;
        }

        if (clamped > 1)
        {
            clamped = 1;
        }

        int filled = (int)Math.Round(clamped * width, MidpointRounding.AwayFromZero);
        return new string(BAR_FILLED, filled) + new string(BAR_EMPTY, width - filled);
    }
}
