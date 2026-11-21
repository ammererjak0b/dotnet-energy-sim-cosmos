# Operation

## Master (`Time.Cli`)
| Key | Action |
|---|---|
| `P` | start, pause, resume |
| `V` | set speed |
| `+` `-` | speed x2, /2 |
| `R` | restart, enter `YYYY-MM-DD HH:MM` (local time) |
| `Q` | quit |

Table: per device status `NEW` (registered, no heartbeat yet), `OK`, `UNREACHABLE`, last seen, skew.

Options: `--headless`, `--port`, `--lead-ms`, `--heartbeat-ms`, `--heartbeat-expiry`, `--start` (UTC ISO), `--speed`.

Headless: no terminal, commands per line on stdin: `start`, `pause`, `resume`, `speed <x>`, `restart <yyyy-MM-dd HH:mm>`, `table`, `quit`.

## Devices (`Cli`)
| Key | Action |
|---|---|
| `O` | on |
| `F` | off |
| `L` | power limit |
| `T` | trigger fault |
| `R` | reset fault |
| `Q` | quit |

With `--clock shared` the clock keys (`P V + -`) are hidden, the master owns them.

Extra keys:
| Device | Keys |
|---|---|
| Battery | `A` auto mode, `M` manual power (negative = charge, positive = discharge) |
| Charger | `C` plug in car, `D` unplug, `N` port fault, `E` clear port fault (each with port 1 or 2) |
| PV, Wind | none |

Options (all overridable as `--Key value`, overview with `--help`):
| Option | Default | Meaning |
|---|---|---|
| `--device` | menu | `pv`, `battery`, `charger`, `wind` |
| `--id` | `<device>-1` | unique id |
| `--clock` | `local` | `local` or `shared` |
| `--location` | `salzburg` | `wien`, `innsbruck` |
| `--start` | `2026-06-21T04:00:00Z` | virtual start time (local clock) |
| `--speed` | 60 | 0.1 to 3600 (local clock) |
| `--weather-url` | `http://localhost:5200` | weather process |
| `--csv` | off | CSV file path |
| `--time-port` | 1502 | master port |
| `--clock-port` | 1601 to 1604 | own clock endpoint |
| `--Modbus:Host` / `Port` / `UnitId` | `0.0.0.0` / 1511 to 1514 / 1 | Modbus server |

Device parameters: `--Pv:PeakPowerKw 10`, `--Battery:CapacityKWh 200`, `--Charger:TotalMaxPowerKw 11`, `--Wind:RatedPowerKw 250`. Full lists: `src/EnergySim.<Device>/README.md`.

Without a terminal (piped input): `--device` required, `quit` ends the run, output is a status log.

CSV: default `data/export/<device>_YYYY_MM_DD.csv` for devices with CSV fields, others only with `--csv`. Existing target is replaced.

## Diagnostic client
```bash
dotnet run --project src/EnergySim.Diagnostic
dotnet run --project src/EnergySim.Diagnostic -- --Host 127.0.0.1 --Port 1512
```
| Command | Effect |
|---|---|
| `header` | 17 header registers, raw and decoded |
| `device` | device values with unit |
| `write on` / `write off` | switch device |
| `write limit <0..100>` | power limit in percent |
| `write reset` | reset fault |
| `watch` | header every second until a key (Ctrl+C when piped) |
| `target [name\|number]` | switch device (pv-1, battery-1, charger-1, wind-1) |
| `quit` | exit |

After a write the header is read again. Warn flag `InvalidWrite` is marked.
Effect of `write limit` shows with the next `header` (state recalculated each tick).

## Gateway
```bash
dotnet run --project src/EnergySim.Gateway -- --PollIntervalMs 500 --Rabbit:Host localhost
```
- Config in `appsettings.json`: `PollIntervalMs` (100 to 60000, default 1000), `UnitId`, `Devices[]` (`Id`, `Type`, `Host`, `Port`), `Rabbit:*`
- Needs `RABBITMQ_USER`, `RABBITMQ_PASSWORD` in the environment
- Publishes only when a device has a new state. Paused: one message at the change, then nothing
- Quit: Ctrl+C or `quit`

## Viewer
```bash
dotnet run --project src/EnergySim.Viewer
dotnet run --project src/EnergySim.Viewer -- --Rabbit:Routing telemetry.pv.*
```
- Prints one block per message: device id, `simTimeUtc`, sequence, quality, state, measurements with unit
- `Bad` and `Uncertain` are marked
- Routing filter: `telemetry.#` (all, default), `telemetry.pv.*` (one type), `telemetry.pv.pv-1` (one device)
- Shows only live data, no backlog
- Quit: Ctrl+C or `quit`

## Reading the output
| Quality | Meaning |
|---|---|
| `Good` | read ok, no warning, clock synced |
| `Uncertain` | master not reachable, or weather stale, or clock unsynced |
| `Bad` | device not reachable or read error, no values, reason in `error` |

Warn flags: `NoMasterSync`, `WeatherStale`, `InvalidWrite`.
