# Docker guide

Run master, weather and a wind simulator as containers. Gateway, viewer and diagnostic client stay native on your machine and use the published ports. Everything here was run and checked on Linux with Docker 29 and Compose 5 (see [[evidence/lb04/wp4-containers|evidence]]).

Back to [[user-documentation/index|User documentation]]

## Who runs what
| Person | Simulator | Service = container = host name | Image | Dockerfile | Ports (host, 127.0.0.1 only) |
|---|---|---|---|---|---|
| Leon | Master (TimeSim) | `master` | `energysim/master:lb04` | `src/EnergySim.Time.Cli/Dockerfile` | 1502 |
| Jakob | WeatherSim | `weather` | `energysim/weather:lb04` | `src/EnergySim.WeatherService/Dockerfile` | 5200 |
| Jonas | Wind (device id `wind-1`) | `wind-1` | `energysim/device:lb04` | `src/EnergySim.Cli/Dockerfile` | 1514 |
| all | RabbitMQ | `rabbitmq` | `rabbitmq:4.3.7-management` | - | 5672, 15672 |

The device image is the same for pv, battery, charger and wind. The device is chosen by the `command` in `compose.yaml`.

## Prerequisites
- Docker + Docker Compose v2 (`docker compose version`)
- Repo root as working directory
- `.env` for RabbitMQ: `cp .env.example .env`, set `RABBITMQ_PASSWORD` (not in git)
- Free ports 1502, 1514, 5200, 5672, 15672 on this machine

## Build
```bash
docker compose build                  # all three simulator images
docker compose build wind-1           # one image
docker compose build --no-cache       # clean rebuild
```
Build context is the repo root (the Dockerfiles copy `src/`). Base images `mcr.microsoft.com/dotnet/sdk:10.0` (build) and `runtime:10.0` (run). First build downloads them, later builds are cached.

## Start
```bash
docker compose up -d --wait           # everything incl. RabbitMQ, returns when all are healthy
docker compose up -d --wait master weather wind-1    # simulators only, no RabbitMQ
```
Start order is handled by `depends_on`: master and weather first (healthy), then `wind-1`.
The master starts the clock by itself (`--autostart`) at 2026-10-23 13:00 UTC, speed 60.

Then, natively in own terminals (repo root):
```bash
set -a; source .env; set +a
dotnet run --project src/EnergySim.Viewer -- --Rabbit:Routing 'telemetry.wind.*'
dotnet run --project src/EnergySim.Gateway           # polls 127.0.0.1:1511..1514, wind-1 is the container
dotnet run --project src/EnergySim.Diagnostic -- --Host 127.0.0.1 --Port 1514
```
The gateway reports the other devices (pv, battery, charger) as not reachable. That is expected unless you start them too.

Native devices cannot join the containerized master: the master dials a device by its id as host name, which does not exist for a program on the host (and the container's loopback is not the host). Port 1502 is published for inspection only. For native devices run the master natively too, or add the device as a container.

## Status
```bash
docker compose ps                     # STATUS must say healthy (master, weather, rabbitmq), wind-1 Up
docker inspect -f '{{.State.ExitCode}}' wind-1
docker logs master | tail             # table: wind-1 @ 1604 | OK | missed 0 | skew 0 ms
curl 'http://127.0.0.1:5200/api/weather?location=salzburg&time=2025-07-01T12:00:00Z'   # weather answers
```

## Logs
```bash
docker compose logs -f                # all, follow
docker compose logs -f wind-1         # one service
docker logs --since 1m master
```
- `master`: `Registered wind-1 on wind-1:1604.`, clock lines, participant table every 5 s
- `wind-1`: one status line per second (state, power, energy)
- `weather`: one line per queried minute

## Manual intervention
| What | How |
|---|---|
| Change wind limit, switch off, fault reset | diagnostic client on port 1514: `write limit 50`, `write off`, `write on`, `write reset` |
| Pause, resume, single step, speed, restart | master commands via `docker attach master`, type `pause`, `step`, `resume`, `speed 600`, `restart 2026-10-23 13:00`, `table`. Detach with Ctrl+P Ctrl+Q (not Ctrl+C) |
| One command from a script | `printf 'pause\n' \| docker attach --sig-proxy=false master` |

Auto mode: the wind simulator needs no input, power follows the weather (wind speed from the weather container at the virtual time). Wind below the cut-in speed shows `Standby` with 0 W, this is correct. The default start time was chosen so it produces power.

## Configuration (all in `compose.yaml`)
| Setting | Where | Meaning |
|---|---|---|
| `--autostart --start <UTC> --speed <x>` | `master.command` | clock starts alone at that virtual time and speed. Remove `--autostart` to start with `start` via attach |
| `--device wind --id wind-1` | `wind-1.command` | device type and id. The service name MUST equal the id |
| `--time-host master` | `wind-1.command` | where the master is (service name) |
| `--clock-host 0.0.0.0` | `wind-1.command` | clock endpoint reachable from the master container |
| `--weather-url http://weather:5200` | `wind-1.command` | where the weather is |
| `--Modbus:Host 0.0.0.0` | `wind-1.command` | Modbus server reachable from outside the container |
| `stdin_open: true` | master, wind-1 | keeps stdin open. Without it a device ends at once (end of stdin = quit) |
| ports `127.0.0.1:...` | each service | published for this machine only, no authentication on Modbus |

Second wind simulator: copy the `wind-1` service block as `wind-2`, change `--id wind-2`, container name and the published host port (`127.0.0.1:1524:1514`). Clock port and Modbus port inside the container stay the same, the master tells the devices apart by host and port. Service name and id stay equal. For the gateway add `{ "Id": "wind-2", "Type": "Wind", "Host": "127.0.0.1", "Port": 1524 }` to its `Devices` (not tested in WP4).

## Network and trust
- No authentication on the Modbus clock endpoints and on the master registration. With `--host 0.0.0.0` / `--clock-host 0.0.0.0` anybody who can reach the port can write clock definitions or register a device. The master logs a warning line when it listens on a non-loopback address. Use it on a private Docker network only
- With `--dial-device-id` the master connects to the host name a registering device announces, so any container on the same network can make it open a connection to a name of its choice. Fine for a team demo network, not for an open network
- Compose creates one bridge network (`<folder>_default`). Containers reach each other by service name: `master`, `weather`, `wind-1`, `rabbitmq`
- Registration: `wind-1` -> `master:1502` (sends port + id). The master then dials `wind-1:1604` (clock endpoint). That is why the service name must equal the device id
- Native programs use `127.0.0.1` and the published ports

## Stop
```bash
docker compose stop                   # SIGTERM to all, exit code 0 (about 1 s)
docker compose down                   # stop + remove containers and network, volume rabbitmq-data stays
docker compose down -v                # also delete the RabbitMQ volume
```
Stop the native viewer, gateway and diagnostic client with Ctrl+C or `quit` first. Check exit codes: `docker inspect -f '{{.Name}} {{.State.ExitCode}}' master weather wind-1` shows 0 for all.

## Troubleshooting
| Symptom | Cause | Fix |
|---|---|---|
| `set in .env` error at `up` | `.env` missing | `cp .env.example .env`, set the password |
| `port is already allocated` | native master, weather or device still running | stop it, or `docker compose down` |
| `wind-1` exits right away, exit 0 | stdin closed | keep `stdin_open: true` |
| `wind-1` exit 1 `weather ... not reachable` | weather container not healthy | `docker compose ps`, `docker logs weather` |
| master log: `Could not initialize wind-1` | service name differs from the id, or `--clock-host` missing | name = id, `--clock-host 0.0.0.0` |
| master log: `UNREACHABLE` | wind-1 stopped | `docker compose up -d wind-1`, it registers again |
| Wind stays `Standby` 0 W | no wind at that virtual time | other `--start` or `--location` in `master.command` / `wind-1.command` |
| Viewer shows nothing | gateway not started or wrong routing | start gateway, `--Rabbit:Routing 'telemetry.#'` |
| Old code in the container | image not rebuilt | `docker compose build`, `docker compose up -d` |
| `docker attach` hangs in a script | piped attach does not end by itself | use `timeout -s KILL 15 docker attach ...` |
