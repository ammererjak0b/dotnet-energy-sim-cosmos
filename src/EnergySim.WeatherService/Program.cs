using System.Globalization;
using System.Net;
using EnergySim.Weather;
using EnergySim.WeatherService;
using Microsoft.Extensions.Configuration;

const int EXIT_OK = 0;
const int EXIT_PORT_IN_USE = 1;
const int EXIT_USAGE = 2;
const int MIN_PORT = 1024; // unprivileged ports only

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true; // clean shutdown instead of kill
    cancellation.Cancel();
};

ConfigurationBuilder builder = new ConfigurationBuilder();
builder.AddCommandLine(args);
IConfiguration configuration = builder.Build();

int port = WeatherApi.DEFAULT_PORT;
string? portText = configuration["port"];
if (!string.IsNullOrWhiteSpace(portText))
{
    if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port < MIN_PORT || port > ushort.MaxValue)
    {
        await Console.Error.WriteLineAsync("error: port '" + portText + "' must be " + MIN_PORT + ".." + ushort.MaxValue);
        await Console.Error.WriteLineAsync("usage: dotnet run --project src/EnergySim.WeatherService -- [--port " + WeatherApi.DEFAULT_PORT + "]");
        return EXIT_USAGE;
    }
}

try
{
    string directory = Path.Combine(AppContext.BaseDirectory, "data");
    IReadOnlyDictionary<string, WeatherDataSet> dataSets = await WeatherDataLoader.loadDirectoryAsync(directory, cancellation.Token);
    WeatherHttpServer server = new WeatherHttpServer(new WeatherModel(dataSets), port, Console.Out);
    await server.runAsync(cancellation.Token);
    return EXIT_OK;
}
catch (HttpListenerException exception)
{
    await Console.Error.WriteLineAsync("error: cannot listen on port " + port + ": " + exception.Message);
    return EXIT_PORT_IN_USE;
}
catch (WeatherDataException exception)
{
    await Console.Error.WriteLineAsync("error: " + exception.Message);
    return EXIT_USAGE;
}
catch (OperationCanceledException)
{
    return EXIT_OK; // Ctrl+C before start finished
}
