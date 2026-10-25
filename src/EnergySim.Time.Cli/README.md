# EnergySim.Time.Cli

Runs the standalone shared time service and its terminal dashboard. Use `dotnet run --project src/EnergySim.Time.Cli` from the repository root. The service is initially stopped and listens on loopback port 1502 by default.

Options: `--port`, `--lead-ms`, `--heartbeat-ms`, `--heartbeat-expiry`, `--start` (UTC ISO timestamp), and `--speed`. Dashboard keys control the clock; `Q` exits the service. Press `R` to enter a restart timestamp as `YYYY-MM-DD HH:MM`; it is interpreted in the local machine's time zone. `--start` sets the initial virtual time.
