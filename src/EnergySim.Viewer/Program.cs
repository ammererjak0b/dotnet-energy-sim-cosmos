using EnergySim.Viewer;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true; // clean shutdown instead of kill
    cancellation.Cancel();
};

return await ViewerRunner.runAsync(args, cancellation);
