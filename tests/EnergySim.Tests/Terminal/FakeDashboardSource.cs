using EnergySim.Core.Devices;
using EnergySim.Terminal;

namespace EnergySim.Tests.Terminal;

public class FakeDashboardSource : IDashboardSource
{
    public FakeDashboardSource(DashboardState state)
    {
        State = state;
    }

    public DashboardState State { get; }

    public List<DeviceCommand> Executed { get; } = new List<DeviceCommand>();

    public int ShutdownCount { get; private set; }

    public DashboardState getState()
    {
        return State;
    }

    public Task<CommandResult> executeAsync(DeviceCommand command, CancellationToken cancellationToken)
    {
        Executed.Add(command);
        return Task.FromResult(new CommandResult(true, "done " + command.Name + " " + command.Value));
    }

    public void requestShutdown()
    {
        ShutdownCount++;
    }
}
