# Requirements

All requirements of EnergySim in one place. Vault root = `docs/`.
Status: open = not done, implemented = in code, verified = checked by test or run with evidence, done = older rows implemented and checked.
IDs: SYS system, TIM time, WTH weather, DEV devices, MOD Modbus, DIA diagnostic, FR-G gateway, FR-V viewer, NFR non-functional, AC acceptance LB03, CON consolidation, TST tests, INT integration test, CTR container, GIT git, EVD evidence, SUB submission (LB04).
Stages stay traceable: LB02 and LB03 rows are kept, LB04 rows are added at the end.

Related: [[technical-documentation/index|Technical documentation]], [[user-documentation/index|User documentation]]

## System (SYS)
| ID | Requirement | Status |
|---|---|---|
| SYS-1 | Distributed system of separate processes on one machine: master, weather, 4 device simulators, diagnostic client, gateway, viewer | done |
| SYS-2 | Device types: PV, battery, charger (2 ports), wind | done |
| SYS-3 | No internet at runtime, weather data lies in the repo | done |
| SYS-4 | New device = 1 project + 1 line in `DeviceFactory` | done |
| SYS-5 | Several instances of one device type possible (own id, own ports) | done |
| SYS-6 | Clean shutdown on Ctrl+C, `Q` or `quit`, exit code 0 | done |
| SYS-7 | Optional CSV history per device | done |
| SYS-8 | Run scripts for Linux (bash) and Windows (PowerShell) with the same steps: whole simulation in own windows (native or Docker mode), headless proof run | Linux verified. Windows: `proof-run.ps1` ran in PowerShell 7 on Linux, `run-simulation.ps1` parsed and its window command checked, not run on Windows ([[evidence/lb04/scripts]]) |

## Time (TIM)
Contract: [[contracts/time]]. Details: [[architecture/modbus-virtual-time]]
| ID | Requirement | Status |
|---|---|---|
| TIM-1 | Master is the only authority for virtual time, devices never integrate their own time | done |
| TIM-2 | Master controls: start, pause, resume, speed, restart with new start time | done |
| TIM-3 | Every control takes effect at one common future instant | done |
| TIM-4 | Pause, resume, speed change keep virtual time continuous. Only restart jumps and increments the generation | done |
| TIM-5 | Tolerance 50 ms virtual between device and master timeline, speed up to 600 | done |
| TIM-6 | Master gone: device keeps running on last definition, warning, retry registration every 1 s | done |
| TIM-7 | Device gone: master marks UNREACHABLE after 3 missed heartbeats, does not block, retries | done |
| TIM-8 | Master shows per device: status NEW/OK/UNREACHABLE, last seen, skew | done |
| TIM-9 | Master headless mode with commands on stdin | done |
| TIM-10 | Device without `--clock shared` runs on a local clock | done |

## Weather (WTH)
Contract: [[contracts/weather]]
| ID | Requirement | Status |
|---|---|---|
| WTH-1 | One HTTP process, stateless, no own clock | done |
| WTH-2 | Reference year 2025, locations `salzburg`, `wien`, `innsbruck`, query year ignored | done |
| WTH-3 | Same location + same minute = same values for all devices (client floors time to the minute) | done |
| WTH-4 | Service down or invalid: last valid value + `stale`. Never a value: PV and wind power 0 | done |
| WTH-5 | Weather service must run at device start (PV, wind), otherwise abort with a hint | done |
| WTH-6 | Source Open-Meteo, CC BY 4.0, credit given | done |

## Devices (DEV)
Models: see `src/EnergySim.<Device>/README.md`
| ID | Requirement | Status |
|---|---|---|
| DEV-1 | Common header: type, state (Off, Standby, Running, Limited, Fault), fault code, warn flags, power, energy, virtual time, sequence, generation | done |
| DEV-2 | Common commands: `O` on, `F` off, `L` limit, `T` fault, `R` fault reset | done |
| DEV-3 | Sign: generation positive, consumption negative | done |
| DEV-4 | Device projects reference only Core: model + adapter, no UI, no I/O | done |
| DEV-5 | PV: power from irradiance, module temperature, performance ratio, limit | done |
| DEV-6 | Battery: SoC window, efficiency, cell temperature, fault on over-temperature, auto schedule and manual power (`A`, `M`) | done |
| DEV-7 | Charger: 2 ports, cars arrive/leave by schedule or key (`C`, `D`), total power cap, taper above SoC threshold, port faults (`N`, `E`) | done |
| DEV-8 | Wind: power curve, storm protection, limit, fault | done |

## Modbus (MOD)
Contract: [[contracts/modbus-registers]]
| ID | Requirement | Status |
|---|---|---|
| MOD-1 | One Modbus TCP server per device process, host, port (1502 or higher), unit id configurable | done |
| MOD-2 | 17-register input header, device block at address 100 + 2 * i, 3 holding registers (on/off, limit, fault reset) | done, read side verified (`DeviceModbusServerTests`) |
| MOD-3 | Big-endian, high word first, 0-based client addresses | done |
| MOD-4 | Server stays reachable while paused, registers keep last state | done |
| MOD-5 | Invalid write: old value restored, warn flag b2 | done, verified (`DeviceModbusServerTests`: value above range queues nothing, flag until next valid write) |
| MOD-6 | Port in use: message, exit code 1. Ports below 1502 rejected | done |
| MOD-7 | EasyModbus (sync) only behind the boundary in `EnergySim.Modbus` | done |

## Diagnostic client (DIA)
| ID | Requirement | Status |
|---|---|---|
| DIA-1 | Read header and device values, write on/off, limit, fault reset | done |
| DIA-2 | Limit range checked before sending | done |
| DIA-3 | Connection lost: readable message, next command reconnects | done |
| DIA-4 | Unknown device type or map version: stop with message | done |

## Gateway (FR-G)
| ID | Requirement | Status |
|---|---|---|
| FR-G1 | Devices configurable by id, type, host, port. Different type reported by the device: error, `Bad` | done, verified for the type check (`GatewayDeviceTests`), config rules (`GatewaySettingsTests`) |
| FR-G2 | Read header (17 registers) and device values, decode per register map | done, verified (`TelemetryMapperTests`, `GatewayDeviceTests`) |
| FR-G3 | Poll interval configurable | done |
| FR-G4 | `simTimeUtc` from register, `polledAtUtc` = poll time, separate fields | done |
| FR-G5 | Publish only on change of `Sequence`, `Generation`, `ClockState`, `State`, fault or warnings. Compare sequence before and after reading (torn read) | done, verified (`GatewayDeviceTests`: same state gives nothing new, new sequence gives a message) |
| FR-G6 | Device failure: one message per outage + `Bad`, no stale values | done, verified (`GatewayDeviceTests`: stopped device gives one Bad, wrong type gives Bad) |
| FR-G7 | Broker failure: reconnect with backoff, message per attempt, count unsent states | done |
| FR-G8 | Clean shutdown (Ctrl+C or `quit`) | done |

## Viewer (FR-V)
| ID | Requirement | Status |
|---|---|---|
| FR-V1 | Binds `telemetry.#`, filter via `--Rabbit:Routing` | done |
| FR-V2 | Shows DeviceId, `simTimeUtc`, `sequence`, `quality`, `state`, measurements with unit | done, verified (`TelemetryFormatTests`) |
| FR-V3 | `Bad` and `Uncertain` visibly marked | done, verified (`TelemetryFormatTests`) |
| FR-V4 | Reconnect on broker failure, clean shutdown. Does not read Modbus | done |

## Messaging
Contract: [[contracts/messaging]]
- AMQP 0-9-1, exchange `energysim.telemetry` (topic, durable), routing key `telemetry.<deviceType>.<deviceId>`
- JSON body, UTF-8, non-persistent
- Quality: `Good`, `Uncertain` (NoMasterSync, WeatherStale, clock Unsynced), `Bad` (read or decode error, no measurements)
- Out of scope: InfluxDB, REST, Grafana, time and weather over RabbitMQ

## Non-functional (NFR)
| ID | Requirement | Status |
|---|---|---|
| NFR-1 | .NET 10, solution builds with 0 warnings, all tests pass | done |
| NFR-2 | LB02: test first (xUnit, no real network, clock or sleep). LB02 integration, LB03 and LB04: no test-first (team decision), tests written after the code, all green before merge | done |
| NFR-3 | Code style per `AGENTS.md`: names, no inline ifs, no syntactic sugar, keyword comments | done |
| NFR-4 | One class per file, no project reference cycles, model never references UI or I/O | done |
| NFR-5 | Console only in `Terminal`, `CliRunner` (startup errors), `Diagnostic`, `Gateway`, `Viewer` | done |
| NFR-6 | All waiting and I/O async with `CancellationToken` | done |
| NFR-7 | No secrets in the repo, RabbitMQ credentials only in `.env` (ignored), `.env.example` versioned | done |
| NFR-8 | `legacy/` is reference only, never changed | done |
| NFR-9 | Contract files and code change together | done |
| NFR-10 | Each project has a `README.md`, development journal entry per piece of work (what, how, which AI, result) | done |

## Acceptance LB03 (AC)
Evidence: logs from `scripts/run-simulation.sh` and `scripts/proof-run.sh`, [[technical-documentation/test-and-evidence]]
| ID | Criterion | Result |
|---|---|---|
| AC-1 | Broker starts from Compose, `docker compose ps` healthy | open |
| AC-2 | All 4 device types arrive in the viewer | open |
| AC-3 | Change a setpoint: visible in simulator, Modbus read and message | open |
| AC-4 | Pause: `simTimeUtc` correct, no new message, resume continues | open |
| AC-5 | Stop a device: `Bad` + error message, restart: `Good` again | open |
| AC-6 | Broker stop/start: reconnect messages, counter, current state after | open |
| AC-7 | All apps and broker stop in a controlled way | open |
| AC-8 | No secret in the repo, `.env.example` exists | open |
| AC-9 | Diagnostic client and the run scripts (`scripts/run-simulation.*`, `scripts/proof-run.*`) still work | open (both scripts ran on Linux, see [[evidence/lb04/scripts]], shared run in WP8) |

Note: the journal reports a run with 4 devices in the viewer, broker stop/start and device stop. Pause test and evidence screenshots are still open, so all ACs stay open until evidence is attached.

## LB04: consolidate, test, containerize
Task text: LB04 "Consolidate, test, containerize project". Out of scope: InfluxDB (LB05), new REST APIs, Grafana. Data path from LB03 stays: device (Modbus) -> gateway -> RabbitMQ -> viewer.
Work plan and owners: [[todo]]. Decisions: [[technical-documentation/decisions]].

### Time control (TIM, LB04 additions)
| ID | Requirement | Status |
|---|---|---|
| TIM-11 | Master command Single step: only while paused, advances virtual time by one device tick (200 ms real x speed), ends paused, same common instant for all devices | verified (unit tests, manual headless run) |
| TIM-12 | Single step available as key `S` and headless command `step` | verified (headless run), key `S` implemented |
| TIM-13 | Master can reach devices by hostname (device id), default stays loopback | verified (codec tests, container run: `Registered wind-1 on wind-1:1604`) |
| TIM-14 | Time endpoints configurable for non-loopback hosts, defaults stay `127.0.0.1` | verified (option tests, container run) |

### Consolidation (CON)
Method: check real state with the coding agent, define problem and how to see the improvement before bigger changes, document briefly (problem, change, check). No full rebuild, consistent areas are only noted as checked.
| ID | Requirement | Status |
|---|---|---|
| CON-1 | Code and architecture check: responsibilities, structure, contracts, config, error handling. Business logic testable without console, network, DB | implemented (check and fixes in [[technical-documentation/consolidation-lb04]]: Gateway, Messaging, Viewer logic now unit tested, viewer text extracted, 4 style fixes). Not every project re-reviewed, see the log |
| CON-2 | User documentation matches the running state: prerequisites, setup, start order, usage, config, limits. Central README + project READMEs | implemented (central README, `docker.md`, `tests/README.md`, Cli README fixed). A member should follow the start order once from scratch |
| CON-3 | Development documentation updated: architecture, modules, interfaces, contracts (time, weather, Modbus registers, RabbitMQ messages), key decisions with reasons | implemented (architecture, decisions, contracts `time.md`, consolidation log). `messaging.md` and `weather.md` checked, unchanged |
| CON-4 | This requirements document updated: status per requirement, earlier stages traceable, no requirement deleted because it is not implemented | implemented (this file, statuses per row, LB02/LB03 rows kept) |
| CON-5 | To-do list consolidated: no duplicates, done items removed, priority, owner, open decisions, reachable by all members and the teacher ([[todo]]) | implemented ([[todo]]) |
| CON-6 | `AGENTS.md` / `CLAUDE.md` updated: structure, working rules, build/test/run commands, team git rules, links to domain docs (no copied content) | implemented, git rules still to be compared with the Moodle guide (`AGENTS.md`: structure, rules, commands, git rules, doc map, links instead of copies, see [[todo]]) |
| CON-7 | Skills from LB03 continued and extended (e.g. SRS vs implementation check), outputs read and verified by a member | implemented (new skill `check-srs-vs-code`, `sync-agent-config` continued). Output must be read by a member |
| CON-8 | Documented list of problems found, changes made, checks done | implemented ([[technical-documentation/consolidation-lb04]]) |

### Git (GIT)
| ID | Requirement | Status |
|---|---|---|
| GIT-1 | Work per task branch, small commits, PR/MR into `main`, per the guide "Git-Workflow im Team" | implemented (branch per ticket `lb04/wpN-...` in use, `lb04/wp1-docs` merged as PR #3, the other PRs come at the end) |
| GIT-2 | Team git rules written into the agent project instructions | implemented, not yet compared with the Moodle guide (`AGENTS.md` section "Git im Team", the agent did not have the guide, open item in [[todo]]) |
| GIT-3 | Changes and test results checked before merge to `main` | open (applies at the merge to `main`: build, tests, diff, review findings) |
| GIT-4 | Never overwrite or discard unfinished changes of other members | implemented as a rule. So far nothing of others was touched: `jakob-ammerer.md` stayed out of every commit |
| GIT-5 | Not versioned: credentials, IDE state, `bin/`, `obj/`, generated build artifacts | verified |

### Tests (TST)
| ID | Requirement | Status |
|---|---|---|
| TST-1 | xUnit test projects in the solution, referencing the production projects, unit and integration clearly separated | verified (`EnergySim.UnitTests`, `EnergySim.IntegrationTests`, label `Category=Integration`) |
| TST-2 | Unit tests for at least one device model: normal operation, edge cases, state changes, expectations justified by the domain (PV off = 0 W, irradiance change, battery energy limits, charge power limit, wind cut-in, storm) | verified (unit tests green, see test-and-evidence) |
| TST-3 | Automated tests for master time control: Start, Pause, Resume, Single step, per the time contract | verified (`TimeAuthorityTests`, `DeviceSimulationLoopTests`) |
| TST-4 | Quality: Arrange/Act/Assert, test names with behavior and expected result, `[Fact]` and `[Theory]`, meaningful assertions, justified float tolerances, controllable time and random sources, independent tests | implemented |
| TST-5 | Unit tests need no running external service. Test doubles only for dependencies, never test the fake itself | implemented |
| TST-6 | Tests run in the IDE and via a documented CLI command, discovered count checked (0 tests is no proof) | CLI verified (`--list-tests` lists both projects), IDE check open |
| TST-7 | Test selection and expectations justified briefly in the docs | implemented (`test-and-evidence.md`) |

### Integration test (INT)
| ID | Requirement | Status |
|---|---|---|
| INT-1 | At least one real server/client test over a real interface (Modbus client reads a started device server) | verified (`DeviceModbusServerTests`, real TCP) |
| INT-2 | Defined initial state, server started by the test or environment documented | verified (test starts its own server on a free port, fixed PV weather sample) |
| INT-3 | Real communication call, at least one domain result compared with a fixed expectation (more than connect or ping) | verified (power 3008 W, device block, limit command, InvalidWrite flag) |
| INT-4 | Timeouts on connect and wait | verified (start 10 s, connect and read 2 s, wait 5 s) |
| INT-5 | Cleanup of processes, connections, resources, also on failure | verified (`IAsyncLifetime.DisposeAsync`, also after a failed assertion) |
| INT-6 | Prerequisites, config, start and execution documented so another member can repeat it | implemented (`tests/README.md`) |

### Container (CTR)
Persons: Jakob WeatherSim, Jonas Wind, Leon Master (TimeSim). No gateway container. Mapping table goes into the central README.
| ID | Requirement | Status |
|---|---|---|
| CTR-1 | Per person one container for an own simulator, joined to the shared simulation over the network | implemented for 3 containers (master, weather, wind-1), joined the system in a one-machine run. Per-person proof in WP8 |
| CTR-2 | Versioned Dockerfile and build guide per simulator | implemented (Dockerfiles, build guide in `docker.md`) |
| CTR-3 | Reproducible start config: settings, network, ports, example configs with placeholders, no local secrets | implemented (`compose.yaml`, `.env.example` has no new secrets) |
| CTR-4 | Documented: observe status, view logs, stop in a controlled way | implemented (`docker.md`: `compose ps`, `logs`, `stop`) |
| CTR-5 | Device container: time and weather connection, Modbus server reachable by diagnostic client and gateway | verified (wind-1: weather + master connection, Modbus read by diagnostic client and gateway) |
| CTR-6 | WeatherSim container: successful weather query via its HTTP interface, used by a device simulator | verified (curl 200, wind-1 uses it) |
| CTR-7 | Master container: devices register and receive clock definitions and heartbeats | verified (wind-1 registered, clock definitions + heartbeats, OK, missed 0) |
| CTR-8 | Auto mode reproducibly configured for the shared run, manual intervention and status check documented | implemented (master `--autostart`, wind follows the weather, manual: diagnostic client, `docker attach master`) |
| CTR-9 | Standalone start and usage without Docker stays possible | verified (native `dotnet run` unchanged, defaults loopback, tests green) |
| CTR-10 | Existing compose file updated, Docker user guide ([[user-documentation/docker]]) | implemented (`compose.yaml` extended, [[user-documentation/docker]]) |
| CTR-11 | Person, simulator, container config mapping in the central README | implemented (central README table) |
| CTR-12 | Portainer as overview of the containers (state, logs, start/stop), local only, no credentials in the repo | implemented (service `portainer` in `compose.yaml`, started and answers on 9443, admin account created by the user at the first visit, guide in `docker.md`) |

### Existing function (keep)
| ID | Requirement | Status |
|---|---|---|
| KEEP-1 | .NET 10, all apps build together via the solution | verified |
| KEEP-2 | Data path all device simulators -> Modbus -> RabbitMQ -> viewer keeps working | open |
| KEEP-3 | Virtual time, weather simulation, error handling, controlled shutdown keep working | partly verified (WP4 run: time, weather, stop exit 0). Full shared run in WP8 |

### Evidence (EVD)
Form: screenshots or short console logs in the repo, linked from the central README. Name config used and commit checked. Separate automated tests, manual checks, code reading only. Disclose failed checks and limits.
| ID | Requirement | Status |
|---|---|---|
| EVD-1 | Consolidation: selected changes, problem and fix, links to SRS, docs, to-do | open |
| EVD-2 | Automated tests: full unit run with count and result, the executed integration test, selection and prerequisites explained | open |
| EVD-3 | Container per person: build and start from versioned config, device or participant id, connection settings, joined the system | open |
| EVD-4 | Data flow: containerized device, one traceable value Modbus -> RabbitMQ -> viewer. Containerized WeatherSim: weather query and its use in a device | open |
| EVD-5 | Manual end-to-end run: change a device state or setpoint, follow it to the viewer, pause, resume, explain mapping to virtual time | open |
| EVD-6 | Controlled shutdown of containers and other apps documented | open |

### Submission (SUB)
| ID | Requirement | Status |
|---|---|---|
| SUB-1 | Annotated tag `lb04-2026`, full commit hash, commit and tag pushed in time | open |
| SUB-2 | Read access for the teacher account | open |
| SUB-3 | Repo has: source + solution, test projects + commands, Dockerfiles + start config, RabbitMQ config + examples, READMEs, user and dev docs, SRS, to-do, `AGENTS.md`/`CLAUDE.md` + skills, member mapping, evidence, journals | open |
| SUB-4 | Personal journals continued (`Done`, `KI`, `Artefact`): consolidation contribution, tests, container, AI suggestions accepted, changed, rejected and how verified | open |

## Open points
- Owners for Messaging, Viewer
- LB03: pause test, evidence screenshots, AC statuses (re-checked in LB04, EVD-5)
- Optional: time and weather over RabbitMQ
- Optional LB04: more domain tests, more integration tests, coverage analysis, more containers, improved skills
