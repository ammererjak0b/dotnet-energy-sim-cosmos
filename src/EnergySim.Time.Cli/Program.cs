using EnergySim.Time.Cli;

CancellationTokenSource cancellation = new CancellationTokenSource();
Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs eventArgs)
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

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
