# Test and evidence

## Automated
```bash
dotnet build EnergySim.sln                          # 0 warnings
dotnet test EnergySim.sln                           # all tests
dotnet test tests/EnergySim.UnitTests               # unit tests only
dotnet test tests/EnergySim.UnitTests --list-tests  # check discovery, count must be > 0
dotnet test tests/EnergySim.UnitTests --filter FullyQualifiedName~EnergySim.UnitTests.Pv   # one area
```
- State after WP6: 618 unit + 9 integration tests green (529 at LB03, +44 WP6 Gateway/Messaging/Viewer, +10 WP2 time control, +6 WP3 PV irradiance, +29 WP4: endpoint and connection host options, registration codec, CLI hosts, headless runner ends on cancel, participant key)
- Project `tests/EnergySim.UnitTests` (renamed from `EnergySim.Tests` in WP3): xUnit, folders per production project, hand-written fakes (`FakeClockSource`, `FakeDevice`, `FakeWeatherClient`, `FakeSimulationSink`, `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`), no network, no real clock, no sleep, seeded randomness
- Integration tests (real server + client) live in their own project `tests/EnergySim.IntegrationTests`, added in WP5. Reason: unit tests need no running service, integration tests open real sockets. Commands and how to repeat: [tests/README.md](../../tests/README.md)
- `Wind/WindWeatherIntegrationTests` (LB02) is a component test: real wind device in the real loop with fake weather and clock. No real interface, so it does not count as the LB04 integration test
- Known: 5 CSV file-lock tests fail on Windows already in the base state (not on Linux)
- Not covered by automated tests: Diagnostic client, Gateway, Viewer, Dockerfiles, compose. Checked manually, see [[evidence/lb04/wp4-containers]]

## Integration test (WP5)
`tests/EnergySim.IntegrationTests/DeviceModbusServerTests.cs`, 4 tests, label `Category=Integration`, evidence [[evidence/lb04/wp5-integration-test]].
- **Interface chosen:** device Modbus server <-> register client. It is the interface the diagnostic client and the gateway use, and the register map is a contract with fixed expected values
- **What:** real `DeviceModbusServer` on a free local port, real PV model with a fixed weather sample published as a frame, real `ModbusRegisterClient` reads header + device block and writes holding registers
- **Fixed expectations:** power 3008 W (PV formula), state Running, virtual time, sequence 7, clock Running, device block 800 / 400 / 600 raw. Valid limit write becomes `SetLimit` 50 %, a write above range queues nothing and sets `InvalidWrite` until the next valid write
- **Timeouts and cleanup:** server start 10 s, client connect/read 2 s, wait for a write 5 s. `IAsyncLifetime.DisposeAsync` frees client and server also after a failure
- **Second class `GatewayDeviceTests` (WP6):** gateway device against the same real server, no broker: new state gives a Good message with PV values, same state again gives nothing, new sequence gives a message, wrong configured type gives one Bad, stopped device gives one Bad
- **Why not more:** one interface, one device profile. The other profiles are the same code with another table (unit tested), the clock endpoints, gateway and containers are covered by manual runs

## Test selection (why these tests)
Rule: test what can go wrong in the domain, expected values come from the model formula or the contract, not from the code under test.
| Area | Tests | Why / expectation source |
|---|---|---|
| PV model (`Pv/PvPlantTests`) | night = 0 W Standby, off = 0 W, power limit, fault, hot cell never negative, daily energy at UTC midnight | formula `P = peak x G/1000 x (1 - 0.004 x (Tcell - 25)) x PR`, `Tcell = Tamb + 0.025 x G` |
| PV irradiance (WP3, Theory) | G = 0, 200, 400, 800, 1000 W/m2 at 20 degC ambient, expected 0, 0.8, 1.568, 3.008, 3.68 kW, tolerance 1e-6 | values computed by hand from the formula. Half irradiance gives MORE than half power because the cell stays cooler |
| Battery (`Battery/*`) | SoC window min/max, efficiency, power limit, over-temperature fault, reset only when cooled, auto schedule | energy balance with 95 % efficiency, SoC window 10 to 90 % from options |
| Charger (`Charger/*`) | port limit, total limit with fair split, taper above 80 % SoC, full car draws 0, port fault stops only that port, sign negative | per-port 11 kW, total 22 kW from options, consumption negative per contract |
| Wind (`Wind/*`) | cut-in, cubic curve, rated, storm cut-out with restart threshold, limit, fault, missing weather | power curve from options, storm protection per README |
| Time control (`Engine/TimeAuthorityTests`) | Start, Pause, Resume, Step (paused only, 200 ms x speed, twice, speed 120, rejected when running/stopped/pending), continuity, restart | `docs/contracts/time.md` |
| Device loop (`Engine/DeviceSimulationLoopTests`) | step = difference of clock readings, pause = 0 step and constant sequence, resume without jump, paused single step = one update, oversized paused jump ignored, stale weather | invariant "device never integrates own time" |
| Clock transport (`Engine/*Codec*`, `Scheduled*`, `Shared*`) | encode/decode, revision order, late command, heartbeat expiry | `docs/architecture/modbus-virtual-time.md` |
| Weather (`Weather/*`) | same minute = same values, interpolation, sun position, outage | `docs/contracts/weather.md` |
| Gateway mapping (`Gateway/TelemetryMapperTests`) | registers to message: Good/Uncertain/Bad rules (NoMasterSync, WeatherStale, Unsynced clock lower the quality, a device fault and `InvalidWrite` do not), measurement order, units, scale, sim time vs polled time, Bad without values | `docs/contracts/messaging.md` quality table, `modbus-registers.md` |
| Gateway settings, Messaging, Viewer | settings errors (poll range, unit id, type, port, duplicate id), JSON camelCase + enum text + null sim time, routing key, broker settings (env credentials), viewer text for Good/Bad/Uncertain | `messaging.md`, Gateway README |
| Terminal, CLI, CSV | render, keys, settings, factory, history rows | usage as documented |

Float tolerance: `Assert.Equal(expected, actual, 6)` (6 decimals) for model formulas, they are pure double arithmetic without accumulation. Energy over several steps uses the same precision.

IDE run: the same tests are discovered in Rider/Visual Studio via the xUnit runner (project file unchanged except the name). Not re-checked in an IDE in WP3, only via CLI.

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
