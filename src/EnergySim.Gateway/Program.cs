using EnergySim.Gateway;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true; // clean shutdown instead of kill
    cancellation.Cancel();
};

return await GatewayRunner.runAsync(args, cancellation);
