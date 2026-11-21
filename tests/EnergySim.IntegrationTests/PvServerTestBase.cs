namespace EnergySim.IntegrationTests;

/// <summary>Start and stop of the PV server for one test. Dispose runs also when the test failed.</summary>
public abstract class PvServerTestBase : IAsyncLifetime
{
    private PvServerHarness? _harness;

    public virtual async Task InitializeAsync()
    {
        _harness = await PvServerHarness.startAsync();
    }

    public virtual Task DisposeAsync()
    {
        if (_harness != null)
        {
            _harness.Dispose();
        }

        return Task.CompletedTask;
    }

    protected PvServerHarness getHarness()
    {
        if (_harness == null)
        {
            throw new InvalidOperationException("server not started");
        }

        return _harness;
    }
}
