using Spectre.Console;

namespace EnergySim.Cli;

/// <summary>Spectre selection menu, used when --device is missing and the terminal is interactive.</summary>
public static class DevicePicker
{
    public static async Task<string> pickAsync(IAnsiConsole console, CancellationToken cancellationToken)
    {
        SelectionPrompt<string> prompt = new SelectionPrompt<string>();
        prompt.Title = "Select device";
        prompt.AddChoices(CliSettings.DEVICE_NAMES);
        return await prompt.ShowAsync(console, cancellationToken);
    }
}
