# EnergySim.Time

`TimeAuthority` owns the shared virtual clock timeline. Controls are scheduled for a common future monotonic instant so connected devices can apply the same revision consistently. `ClockServiceHost` coordinates local Modbus participants, sends definitions and heartbeats, and removes endpoints that remain unreachable so they can register again.

`TimeServiceOptions` configures the local TCP port, scheduling lead, heartbeat interval, expiry policy, initial virtual UTC time, and speed. The service is intended to run independently of a future master simulator; a master can later coordinate it through the authority API.

## Reuse from a master simulator

`EnergySim.Time.Cli` is a standalone development and demonstration host. A future master should create `TimeAuthority` and `ClockServiceHost` directly and own their startup, shutdown, and control commands. It should not invoke `TimeServiceRunner`, which parses command-line options and creates a separate authority instance.

The generic `DeviceDashboard` in `EnergySim.Terminal` is reusable by both applications. `TimeServiceDashboardSource` currently lives in the standalone CLI; a master can provide its own `IDashboardSource` or move that adapter into a shared UI project if it wants the same clock screen. The current device `CliRunner` is an executable composition root, not a device-launch API for a master.
