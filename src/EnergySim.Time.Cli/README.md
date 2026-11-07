# EnergySim.Time.Cli

Runs the standalone shared time service and its terminal dashboard. Use `dotnet run --project src/EnergySim.Time.Cli` from the repository root. The service is initially stopped and listens on loopback port 1502 by default.

Options: `--headless`, `--port`, `--lead-ms`, `--heartbeat-ms`, `--heartbeat-expiry`, `--start` (UTC ISO timestamp), and `--speed`. Dashboard keys control the clock; `Q` exits the service. Press `R` to enter a restart timestamp as `YYYY-MM-DD HH:MM`; it is interpreted in the local machine's time zone. `--start` sets the initial virtual time.

## Headless mode

`--headless` needs no terminal. It prints clock changes, service log lines and a participant table (status, last seen, missed heartbeats, skew) as plain lines, the table at least every 5 s. Commands are read from stdin, one per line: `start`, `pause`, `resume`, `speed <x>`, `restart <yyyy-MM-dd HH:mm>` (local time), `table`, `quit`. Ctrl+C and `quit` stop cleanly with exit code 0. A closed stdin keeps the service running until Ctrl+C.

## Exit codes and failures

- 0: clean stop
- 1: port in use (message names the port; choose another `--port`)
- 2: invalid option or other error (service port must be 1502 to 65535)

Simulator unreachable, master restart and duplicate id are shown in the table and log, see `src/EnergySim.Time/README.md`. The service endpoint stays loopback-only.
