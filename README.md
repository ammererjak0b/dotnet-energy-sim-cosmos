# EnergySim

EnergySim is a set of .NET device simulators for photovoltaic generation, battery storage, and an EV charger. The CLI can use a local clock by default or join a standalone shared time service over loopback Modbus TCP.

## Shared time service

For the full clock model, Modbus register map, device registration sequence, heartbeat behavior, and process flow, see [Modbus and virtual-time architecture](docs/architecture/modbus-virtual-time.md).

Start the service with `dotnet run --project src/EnergySim.Time.Cli`. It binds to `127.0.0.1:1502` by default. The service provides controls for start, pause, resume, speed changes, and restart. Press `R` and enter a timestamp as `YYYY-MM-DD HH:MM` to choose the reset time; it uses the local machine's time zone. Configure the service port and timing with `--port`, `--lead-ms`, `--heartbeat-ms`, `--heartbeat-expiry`, `--start`, and `--speed`.

Start a device with shared time using `dotnet run --project src/EnergySim.Cli -- --device pv --clock shared`. Device options include `--time-host 127.0.0.1`, `--time-port 1502`, and `--clock-port`; device-type defaults use ports 1601, 1602, and 1603. Each device runs as a separate local process and registers its own Modbus endpoint.

The time service sends versioned clock definitions on registration and when a control action takes effect. It sends small heartbeats periodically; devices extrapolate virtual time between messages using a monotonic system clock. If heartbeats expire, an initialized device continues with its last known clock settings, displays a connection warning, and retries registration. When the service returns, its time, speed, and status become authoritative again. A new service session resets the device to that session's timeline; a restarted service is initially stopped until you press `P`. The service only listens on loopback because this assignment targets one local machine.

## Projects

- `EnergySim.Core`: device contracts, clock sources, simulation loop, and history.
- `EnergySim.Modbus`: local Modbus TCP transport and clock message codecs.
- `EnergySim.Time`: time authority and participant coordination.
- `EnergySim.Time.Cli`: standalone time service UI.
- `EnergySim.Cli`: device simulator UI and process composition.
- `EnergySim.Tests`: deterministic unit tests.

`legacy/` contains reference implementations and is not part of the active build workflow.

## Future master simulator

`EnergySim.Time.Cli` is a standalone development host. A future master should create `TimeAuthority` and `ClockServiceHost` directly, then own their lifecycle and controls; it should not invoke the time CLI runner, which creates its own authority. `EnergySim.Terminal` provides the shared dashboard framework. See [EnergySim.Time's reuse guidance](src/EnergySim.Time/README.md#reuse-from-a-master-simulator) for the current integration boundary and UI options.
