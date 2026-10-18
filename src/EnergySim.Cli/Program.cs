using EnergySim.Cli;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true; // clean shutdown instead of kill
    cancellation.Cancel();
};

return await CliRunner.runAsync(args, cancellation.Token);
