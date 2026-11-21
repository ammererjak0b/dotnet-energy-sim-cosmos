# User documentation

How to run and use EnergySim. Requirements: [[requirements]]. Internals: [[technical-documentation/index|Technical documentation]].

| Note | Content |
|---|---|
| [[user-documentation/operation\|Operation]] | Keys, commands, options of every program |
| [[user-documentation/troubleshooting\|Troubleshooting]] | Errors, ports, limits |

## What it is
Simulated energy system: PV, battery, charger (2 ports), wind. Each device is its own process with a Modbus server. A master process owns the virtual time, a weather process feeds PV and wind. Optional: gateway sends device data to RabbitMQ, viewer prints it.

## Prerequisites
- .NET 10 SDK
- Docker (only for RabbitMQ, LB03)
- No internet needed

## Build
```bash
dotnet build EnergySim.sln
dotnet test EnergySim.sln
```

## Quick start: everything in own windows
```bash
scripts/testrun_task2.sh          # master, weather, 4 devices, diagnostic. "pv2" adds a second PV, "stop" ends all
cp .env.example .env              # once, set RABBITMQ_PASSWORD
scripts/testrun_task3.sh          # same + broker, gateway, viewer. "stop" ends all
```
Then press `P` in the master window: clock starts.

## Manual start (repo root, one terminal each, this order)
1. Master (clock stays stopped until `P`)
   ```bash
   dotnet run --project src/EnergySim.Time.Cli
   ```
2. Weather service (PV and wind abort without it)
   ```bash
   dotnet run --project src/EnergySim.WeatherService
   ```
3. Devices
   ```bash
   dotnet run --project src/EnergySim.Cli -- --device pv --clock shared
   dotnet run --project src/EnergySim.Cli -- --device battery --clock shared
   dotnet run --project src/EnergySim.Cli -- --device charger --clock shared
   dotnet run --project src/EnergySim.Cli -- --device wind --clock shared
   ```
   Second instance of a type: own id and both ports
   ```bash
   dotnet run --project src/EnergySim.Cli -- --device pv --id pv-2 --clock shared --clock-port 1611 --Modbus:Port 1521
   ```
   Without `--clock shared` the device runs alone on a local clock.
4. Diagnostic client (optional)
   ```bash
   dotnet run --project src/EnergySim.Diagnostic
   ```
5. RabbitMQ, viewer, gateway (optional)
   ```bash
   cp .env.example .env                 # once, set the password
   docker compose up -d --wait
   set -a; source .env; set +a          # credentials into the shell
   dotnet run --project src/EnergySim.Viewer
   dotnet run --project src/EnergySim.Gateway
   ```
   Broker UI: `http://localhost:15672` (login from `.env`)

## Stop
Ctrl+C or `Q` / `quit` in each program, reverse order. `docker compose stop` keeps data, `docker compose down` removes the container (`-v` also the volume).

## Default ports
| Program | Port |
|---|---|
| Master | 1502 |
| Weather | 5200 |
| Device clock endpoints | 1601 pv, 1602 battery, 1603 charger, 1604 wind |
| Device Modbus servers | 1511 pv, 1512 battery, 1513 charger, 1514 wind |
| RabbitMQ | 5672, UI 15672 |
