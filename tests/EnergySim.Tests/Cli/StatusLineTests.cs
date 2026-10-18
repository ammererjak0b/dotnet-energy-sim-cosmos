using EnergySim.Cli;
using EnergySim.Terminal;
using EnergySim.Core.Devices;

namespace EnergySim.Tests.Cli;

public class StatusLineTests
{
    [Fact]
    public void Format_ShowsDeviceTimeSpeedAndHeaderValues()
    {
        // Arrange
        DashboardState state = new DashboardState();
        state.DeviceId = "pv-1";
        state.ClockText = "2026-06-21 13:00 CEST (11:00Z)";
        state.ClockStatus = "Running";
        state.Speed = 3600;
        state.HeaderRows.Add(DeviceSupport.createRow("State", "Running"));
        state.HeaderRows.Add(DeviceSupport.createRow("Active power", "4.20 kW"));

        // Act
        string line = StatusLine.format(state);

        // Assert
        Assert.Equal("pv-1 | 2026-06-21 13:00 CEST (11:00Z) | Clock Running x3600 | State Running | Active power 4.20 kW", line);
    }
}
