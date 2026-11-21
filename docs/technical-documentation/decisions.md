# Decisions

## LB01 to LB02 (2026-10)
| Decision | Reason |
|---|---|
| Merge 3 LB01 repos into one, old code in `legacy/<name>/` with history | one solution, history kept |
| Slim core (8 projects) instead of a 25-assembly agent draft | draft was out of scope, discarded completely. Lesson: define clearly what the agent should do |
| One `Cli` executable for all devices | each simulator is still its own process, no copied start code. New device = 1 project + 1 line |
| Device projects reference only `Core` | model + adapter, testable without UI or I/O |
| Spectre.Console only in `Terminal` | one UI for all |

## LB02 integration (2026-11-07)
| Decision | Reason |
|---|---|
| Master is the only time authority | devices never drift apart |
| Definition at a future common instant + heartbeat | all devices switch together, master loss is visible |
| Device keeps last definition if master is gone | survives master restart |
| Weather as own HTTP process, stateless | one weather for all |
| Client floors time to the minute | two devices with offset ticks get identical values |
| Weather outage: last valid value + `stale` | simulation keeps running |
| Register contract in `docs/contracts`, code is the source of truth | code and contract change together |
| Invalid write: accept, restore, warn flag b2 | EasyModbus cannot return exception 03 |
| Port in use: probe with `TcpListener` first | `Listen()` does not throw, bind error would kill the process |
| One agent per module in own git worktree and branch, merged into `teamarbeit` | parallel work, each agent saw only its files and the contract |
| Tests added afterwards instead of test-first | team decision. Later the integration-phase tests were removed again, `testrun_task2.sh` replaces them (commit 4d807f4) |
| Tolerance 50 ms virtual (measured 0 to -16 ms) | sampling latency times speed, no drift |

## LB03 (2026-11-08)
| Decision | Reason |
|---|---|
| AMQP 0-9-1 (`RabbitMQ.Client` 7.x) | exchange/routing/binding fits the task, async, no plugin. MQTT and AMQP 1.0 rejected (1.0 forces persistent messages) |
| One gateway process for all devices | simple, errors handled per device |
| Broker `rabbitmq:4.3.7-management`, pinned | no `latest`, credentials via `.env` only |
| Ports bound to `127.0.0.1` in Compose | this machine only |
| `HeaderSnapshot` moved into `Modbus` | shared by diagnostic and gateway (DRY) |
| `Messaging` is the only project with `RabbitMQ.Client` | broker library stays behind one boundary |
| Viewer queue exclusive, auto-delete, no backlog | viewer shows live data only |
| Publish only on new state | pause produces no spam, one message on state change |
| `Bad` message once per outage, no old values | no fake data |
| No test-first in LB03 | team decision |
| Agent skills in `.agents/skills`, copy in `.claude/skills` | agent neutral (Claude, Codex, Copilot) |

## LB04 (2026-11-21)
| Decision | Reason |
|---|---|
| Lift the loopback limit for time endpoints, defaults stay `127.0.0.1` | containers must join the master over a Docker network, standalone start stays unchanged |
| Master dials the device id as hostname (compose service name = device id) | registration carries only a port (holding register 0), no spike for the client IP, KISS |
| Single step = one device tick (200 ms real x speed), only while paused | needs no new unit, same common-instant mechanism as other controls |
| Step is ONE definition: status stays Paused, virtual anchor jumps forward. Not resume + pause | `TimeAuthority` allows one pending change, devices need revision + 1 per change. Two timed changes would be fragile |
| Device loop: step = difference of two clock readings in every state (was: only while Running) | a forward jump while paused must reach the model as one step. Paused time stands still, so the difference stays 0 without a jump. Clamp (`MaxStepSeconds`) and backward guard stay. Code review finding: while not running only a jump up to 200 ms x speed counts, bigger jumps are ignored with a warning |
| Containers: Jakob WeatherSim, Jonas Wind, Leon Master | keeps module ownership traceable |
| No gateway container | not required, gateway reaches containers over published ports |
| Test projects split: unit and integration. WP3 renames the existing project to `EnergySim.UnitTests`, the integration project is created in WP5 together with its first test | clear separation as required, an empty project with 0 tests would prove nothing |
| Hand-written fakes, no FakeItEasy | matches `AGENTS.md`, fakes already exist |
| No test-first in LB04: code first, then all tests, then run and fix | team decision |
| To-do list as repo file `docs/todo.md` | reachable by all members and the teacher, versioned |
| Registration carries port + device id (19 registers), the old 1-register form stays accepted | the master only learns the id by dialing the device, but needs the id to know the host name. EasyModbus does not expose the client IP at a write, so an announced id is the simple way. Old form keeps old devices working |
| Master option `--dial-device-id`, default off | loopback stays the default for native runs. Compose service name = device id is then the DNS name |
| Bind addresses by option (`--host`, `--clock-host`), default `127.0.0.1`; `--time-host` any host name or IP | containers must listen on `0.0.0.0` and find the master by name, native runs unchanged |
| Master option `--autostart` | an unattended container run needs a started clock, no manual `start` over attach |
| One image for all four devices, device chosen by compose `command` | no copied Dockerfile, same as one `Cli` executable |
| Runtime image `dotnet/runtime:10.0`, not `aspnet` | `HttpListener` (weather) and Modbus need no ASP.NET, smaller image |
| Compose healthchecks with bash `/dev/tcp` | image has no curl, bash is there. `wind-1` waits for healthy master and weather |
| `stdin_open: true` for master and devices | headless devices end at end of stdin |
| SIGTERM handled like Ctrl+C in Cli, Time.Cli, WeatherService | `docker stop` sends SIGTERM, exit code 0 instead of 143/137 |
| Fix: headless stdin reader runs in `Task.Run` (found by the container test: wind-1 exit 137) | `Console.In` reads synchronously and blocked `HeadlessRunner.runAsync`, so Ctrl+C or SIGTERM never ended a headless device with open stdin. Old bug, unit test `HeadlessRunnerTests` added (the test hangs without the fix) |
| Gateway, viewer, diagnostic stay native | decided earlier: no gateway container |
| Each WP is a ticket: work, check by the user, then commit and push | traceable merges, nothing lands unchecked |
