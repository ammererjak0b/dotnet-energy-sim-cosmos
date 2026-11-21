# EnergySim.Time.Cli

| | |
|---|---|
| What | master executable: dashboard or `--headless`, port 1502, keys P S V + - R Q, container image |
| Type | exe |
| Uses | `Time`, `Terminal` |
| Used by | nobody (exe) |
| Owner | Leon |
| Start | `dotnet run --project src/EnergySim.Time.Cli` |
| Tests | `tests/EnergySim.UnitTests/Engine` |
| Docs | `docs/contracts/time.md`, `docs/user-documentation/docker.md` |


Runs the standalone shared time service and its terminal dashboard. Use `dotnet run --project src/EnergySim.Time.Cli` from the repository root. The service is initially stopped and listens on loopback port 1502 by default.

Options: `--headless`, `--host <ip>` (bind address, default `127.0.0.1`, `0.0.0.0` in a container), `--dial-device-id` (dial devices by their id as host name, containers), `--autostart` (start the clock right after startup), `--port`, `--lead-ms`, `--heartbeat-ms`, `--heartbeat-expiry`, `--start` (UTC ISO timestamp), and `--speed`. Dashboard keys control the clock (`P` start/pause/resume, `S` single step while paused, `V` `+` `-` speed, `R` restart); `Q` exits the service. Press `R` to enter a restart timestamp as `YYYY-MM-DD HH:MM`; it is interpreted in the local machine's time zone. `--start` sets the initial virtual time.

## Headless mode

`--headless` needs no terminal. It prints clock changes, service log lines and a participant table (status, last seen, missed heartbeats, skew) as plain lines, the table at least every 5 s. Commands are read from stdin, one per line: `start`, `pause`, `resume`, `step` (only while paused, one device tick x speed), `speed <x>`, `restart <yyyy-MM-dd HH:mm>` (local time), `table`, `quit`. Ctrl+C and `quit` stop cleanly with exit code 0. A closed stdin keeps the service running until Ctrl+C.

## Exit codes and failures

- 0: clean stop
- 1: port in use (message names the port; choose another `--port`)
- 2: invalid option or other error (service port must be 1502 to 65535)

Simulator unreachable, master restart and duplicate id are shown in the table and log, see `src/EnergySim.Time/README.md`. The service endpoint binds loopback by default, `--host 0.0.0.0` opens it for a Docker network. Container: `Dockerfile` in this folder, guide `docs/user-documentation/docker.md`. SIGTERM (`docker stop`) ends it like Ctrl+C, exit code 0.
