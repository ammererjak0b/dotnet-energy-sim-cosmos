using System.Runtime.InteropServices;
using EnergySim.Cli;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true; // clean shutdown instead of kill
    cancellation.Cancel();
};

using PosixSignalRegistration sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, delegate (PosixSignalContext context)
{
    context.Cancel = true; // docker stop sends SIGTERM: clean shutdown like Ctrl+C, exit code 0
    cancellation.Cancel();
});

return await CliRunner.runAsync(args, cancellation.Token);
