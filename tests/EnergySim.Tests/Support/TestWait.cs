namespace EnergySim.Tests.Support;

/// <summary>Bounded wait for background tasks (fake time cannot drive thread pool continuations).</summary>
public static class TestWait
{
    private const int MAX_ATTEMPTS = 500;

    private const int ATTEMPT_DELAY_MILLISECONDS = 10;

    public static async Task untilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(ATTEMPT_DELAY_MILLISECONDS);
        }
    }
}
