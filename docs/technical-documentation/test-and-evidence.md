# Test and evidence

## Automated
```bash
dotnet build EnergySim.sln   # 0 warnings
dotnet test EnergySim.sln    # 538 tests green after WP2 (529 at LB03)
```
- xUnit in `tests/EnergySim.Tests`, folders per project, hand-written fakes, no network, no real clock, no sleep
- Known: 5 CSV file-lock tests fail on Windows already in the base state (not on Linux)
- Diagnostic client and Gateway/Viewer have no own automated tests

## Manual run scripts
| Script | Does |
|---|---|
| `scripts/proof-run.sh <logdir>` | headless LB02 proof run, 10 processes, writes logs |
| `scripts/testrun_task2.sh` | master, weather, 4 devices, diagnostic in own terminal windows (`pv2` adds a second PV, `stop` ends all) |
| `scripts/testrun_task3.sh` | same + RabbitMQ, gateway, viewer (`stop` ends all, needs `.env`, docker, a terminal emulator) |

## Evidence
- LB02 proof run (time sync, pause, resume, restart, weather, diagnostic, shutdown): [[evidence/README]], logs in `docs/evidence/logs`
- Wind: [[evidence/wind-weather-contract]], `docs/evidence/wind-local-run.log`
- LB03: run done (4 devices in viewer, broker stop/start, device stop gives `Bad`). Open: pause test, screenshots. Acceptance table: [[requirements]]
