using System.Runtime.InteropServices;
using EnergySim.Time.Cli;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using PosixSignalRegistration sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, delegate (PosixSignalContext context)
{
    context.Cancel = true; // docker stop sends SIGTERM: clean shutdown like Ctrl+C, exit code 0
    cancellation.Cancel();
});

try
{
    return await TimeServiceRunner.runAsync(args, cancellation.Token);
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception exception)
{
    await Console.Error.WriteLineAsync("error: " + exception.Message);
    return 2;
}
