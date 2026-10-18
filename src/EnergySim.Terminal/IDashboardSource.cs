using EnergySim.Core.Devices;

namespace EnergySim.Terminal;

/// <summary>What the dashboard needs from the host: state, command execution, shutdown.</summary>
public interface IDashboardSource
{
    DashboardState getState();

    Task<CommandResult> executeAsync(DeviceCommand command, CancellationToken cancellationToken);

    void requestShutdown();
}
