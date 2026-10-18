using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace EnergySim.Tests.Terminal;

/// <summary>Renders a block into plain text (no colors) for assertions.</summary>
public static class RenderHelper
{
    public static string render(IRenderable renderable, int width = 100)
    {
        TestConsole console = new TestConsole();
        console.Profile.Width = width;
        console.Write(renderable);
        return console.Output;
    }
}
