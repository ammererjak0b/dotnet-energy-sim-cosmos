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
| MOD-2 | 17-register input header, device block at address 100 + 2 * i, 3 holding registers (on/off, limit, fault reset) | done |
| MOD-3 | Big-endian, high word first, 0-based client addresses | done |
| MOD-4 | Server stays reachable while paused, registers keep last state | done |
| MOD-5 | Invalid write: old value restored, warn flag b2 | done |
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
| FR-G1 | Devices configurable by id, type, host, port. Different type reported by the device: error, `Bad` | done |
| FR-G2 | Read header (17 registers) and device values, decode per register map | done |
| FR-G3 | Poll interval configurable | done |
| FR-G4 | `simTimeUtc` from register, `polledAtUtc` = poll time, separate fields | done |
| FR-G5 | Publish only on change of `Sequence`, `Generation`, `ClockState`, `State`, fault or warnings. Compare sequence before and after reading (torn read) | done |
| FR-G6 | Device failure: one message per outage + `Bad`, no stale values | done |
| FR-G7 | Broker failure: reconnect with backoff, message per attempt, count unsent states | done |
| FR-G8 | Clean shutdown (Ctrl+C or `quit`) | done |

## Viewer (FR-V)
| ID | Requirement | Status |
|---|---|---|
| FR-V1 | Binds `telemetry.#`, filter via `--Rabbit:Routing` | done |
| FR-V2 | Shows DeviceId, `simTimeUtc`, `sequence`, `quality`, `state`, measurements with unit | done |
| FR-V3 | `Bad` and `Uncertain` visibly marked | done |
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
Evidence: logs from `scripts/testrun_task3.sh`, [[technical-documentation/test-and-evidence]]
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
| AC-9 | Diagnostic client and `testrun_task2.sh` still work | open |

Note: the journal reports a run with 4 devices in the viewer, broker stop/start and device stop. Pause test and evidence screenshots are still open, so all ACs stay open until evidence is attached.

## LB04: consolidate, test, containerize
Task text: LB04 "Consolidate, test, containerize project". Out of scope: InfluxDB (LB05), new REST APIs, Grafana. Data path from LB03 stays: device (Modbus) -> gateway -> RabbitMQ -> viewer.
Work plan and owners: [[todo]]. Decisions: [[technical-documentation/decisions]].

### Time control (TIM, LB04 additions)
| ID | Requirement | Status |
|---|---|---|
| TIM-11 | Master command Single step: only while paused, advances virtual time by one device tick (200 ms real x speed), ends paused, same common instant for all devices | verified (unit tests, manual headless run) |
| TIM-12 | Single step available as key `S` and headless command `step` | verified (headless run), key `S` implemented |
| TIM-13 | Master can reach devices by hostname (device id), default stays loopback | open |
| TIM-14 | Time endpoints configurable for non-loopback hosts, defaults stay `127.0.0.1` | open |

### Consolidation (CON)
Method: check real state with the coding agent, define problem and how to see the improvement before bigger changes, document briefly (problem, change, check). No full rebuild, consistent areas are only noted as checked.
| ID | Requirement | Status |
|---|---|---|
| CON-1 | Code and architecture check: responsibilities, structure, contracts, config, error handling. Business logic testable without console, network, DB | open |
| CON-2 | User documentation matches the running state: prerequisites, setup, start order, usage, config, limits. Central README + project READMEs | open |
| CON-3 | Development documentation updated: architecture, modules, interfaces, contracts (time, weather, Modbus registers, RabbitMQ messages), key decisions with reasons | open |
| CON-4 | This requirements document updated: status per requirement, earlier stages traceable, no requirement deleted because it is not implemented | open |
| CON-5 | To-do list consolidated: no duplicates, done items removed, priority, owner, open decisions, reachable by all members and the teacher ([[todo]]) | open |
| CON-6 | `AGENTS.md` / `CLAUDE.md` updated: structure, working rules, build/test/run commands, team git rules, links to domain docs (no copied content) | open |
| CON-7 | Skills from LB03 continued and extended (e.g. SRS vs implementation check), outputs read and verified by a member | open |
| CON-8 | Documented list of problems found, changes made, checks done | open |

### Git (GIT)
| ID | Requirement | Status |
|---|---|---|
| GIT-1 | Work per task branch, small commits, PR/MR into `main`, per the guide "Git-Workflow im Team" | open |
| GIT-2 | Team git rules written into the agent project instructions | open |
| GIT-3 | Changes and test results checked before merge to `main` | open |
| GIT-4 | Never overwrite or discard unfinished changes of other members | open |
| GIT-5 | Not versioned: credentials, IDE state, `bin/`, `obj/`, generated build artifacts | verified |

### Tests (TST)
| ID | Requirement | Status |
|---|---|---|
| TST-1 | xUnit test projects in the solution, referencing the production projects, unit and integration clearly separated | implemented for unit tests (`EnergySim.UnitTests`), integration project follows in WP5 |
| TST-2 | Unit tests for at least one device model: normal operation, edge cases, state changes, expectations justified by the domain (PV off = 0 W, irradiance change, battery energy limits, charge power limit, wind cut-in, storm) | verified (545 unit tests green) |
| TST-3 | Automated tests for master time control: Start, Pause, Resume, Single step, per the time contract | verified (`TimeAuthorityTests`, `DeviceSimulationLoopTests`) |
| TST-4 | Quality: Arrange/Act/Assert, test names with behavior and expected result, `[Fact]` and `[Theory]`, meaningful assertions, justified float tolerances, controllable time and random sources, independent tests | implemented |
| TST-5 | Unit tests need no running external service. Test doubles only for dependencies, never test the fake itself | implemented |
| TST-6 | Tests run in the IDE and via a documented CLI command, discovered count checked (0 tests is no proof) | CLI verified (`--list-tests`, 545 run), IDE check open |
| TST-7 | Test selection and expectations justified briefly in the docs | implemented (`test-and-evidence.md`) |

### Integration test (INT)
| ID | Requirement | Status |
|---|---|---|
| INT-1 | At least one real server/client test over a real interface (Modbus client reads a started device server) | open |
| INT-2 | Defined initial state, server started by the test or environment documented | open |
| INT-3 | Real communication call, at least one domain result compared with a fixed expectation (more than connect or ping) | open |
| INT-4 | Timeouts on connect and wait | open |
| INT-5 | Cleanup of processes, connections, resources, also on failure | open |
| INT-6 | Prerequisites, config, start and execution documented so another member can repeat it | open |

### Container (CTR)
Persons: Jakob WeatherSim, Jonas Wind, Leon Master (TimeSim). No gateway container. Mapping table goes into the central README.
| ID | Requirement | Status |
|---|---|---|
| CTR-1 | Per person one container for an own simulator, joined to the shared simulation over the network | open |
| CTR-2 | Versioned Dockerfile and build guide per simulator | open |
| CTR-3 | Reproducible start config: settings, network, ports, example configs with placeholders, no local secrets | open |
| CTR-4 | Documented: observe status, view logs, stop in a controlled way | open |
| CTR-5 | Device container: time and weather connection, Modbus server reachable by diagnostic client and gateway | open |
| CTR-6 | WeatherSim container: successful weather query via its HTTP interface, used by a device simulator | open |
| CTR-7 | Master container: devices register and receive clock definitions and heartbeats | open |
| CTR-8 | Auto mode reproducibly configured for the shared run, manual intervention and status check documented | open |
| CTR-9 | Standalone start and usage without Docker stays possible | open |
| CTR-10 | Existing compose file updated, Docker user guide ([[user-documentation/docker]]) | open |
| CTR-11 | Person, simulator, container config mapping in the central README | open |

### Existing function (keep)
| ID | Requirement | Status |
|---|---|---|
| KEEP-1 | .NET 10, all apps build together via the solution | verified |
| KEEP-2 | Data path all device simulators -> Modbus -> RabbitMQ -> viewer keeps working | open |
| KEEP-3 | Virtual time, weather simulation, error handling, controlled shutdown keep working | open |

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
