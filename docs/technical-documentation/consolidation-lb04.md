# Consolidation LB04

Check of the real project state, done with the coding agent in WP6 (2026-11-21, branch `lb04/wp6-consolidation`). Method: look at the repo as it is, name the problem and how we will see the improvement, change only what is a real problem, note what was checked and found fine. Requirements: CON-1 to CON-8 in [[requirements]]. Ticket list: [[todo]].

Results of the agent were read by the person who started the run, not yet by the other two members.

## Problems found and what was done
| # | Area | Problem (how we saw it) | Change | Check (improvement visible as) |
|---|---|---|---|---|
| 1 | Code, tests | `Gateway`, `Messaging`, `Viewer` had no tests at all (`grep` in `tests/`: no reference to the three projects), although their logic is pure or testable over a real server | unit tests: `TelemetryMapperTests`, `GatewaySettingsTests`, `TelemetryJsonTests`, `RoutingTests`, `RabbitOptionsTests`, `TelemetryFormatTests`. Integration tests: `GatewayDeviceTests` (gateway device against a real device Modbus server, no broker) | tests green. A deliberate change of the quality rule in `TelemetryMapper` made a test fail, then reverted |
| 2 | Code, boundary | Viewer text formatting was a private method that wrote to `Console` inside `ViewerRunner`, not testable without console | extracted `TelemetryFormat.format(message)` (pure, returns text), runner only prints it | `TelemetryFormatTests` need no console |
| 3 | Code, style | 4 places broke the `AGENTS.md` rules: lambda in `GatewaySettings`, ternary in `CsvHistoryWriter`, two lambdas in `DeviceDashboard` | plain loop with `HashSet`, `if` block, `delegate` | build 0 warnings, all tests green. Test code and the `Wind` tests still contain lambdas in a few places, left as is |
| 4 | Test structure | Integration test setup (free port, server, PV frame) was inside one test class, a second integration class would copy it | `PvServerHarness` shared by both integration classes | both classes use it, 9 integration tests green |
| 5 | Docs | central `README.md` intro said "LB02", test commands had no integration project, Docker prerequisite missing | intro, prerequisites, test commands, link to docs vault | read against the repo |
| 6 | Docs | `src/EnergySim.Cli/README.md` still said "Wind-Anbindung ... werden im Team festgelegt" and "Fünf bekannte Tests scheitern" without context | rewritten to the current state (owner table is in the central README, Windows note explained) | read against the repo |
| 7 | Docs | `docs/technical-documentation/workflow.md` listed `lb03` as the current branch and TDD as the only policy | branches and test policy of LB04 | read |
| 8 | Docs | hard-coded test counts in `tests/README.md` and `requirements.md` went stale with every new test (found by the code review of WP5) | counts only in the evidence files and the "state after" line, elsewhere "more than 0" | `grep` for counts |
| 9 | SRS | rows of older stages had status `done` without naming the proof, although tests now exist | LB03 and LB02 rows (MOD-2, MOD-5, FR-G1, FR-G2, FR-G5, FR-G6, FR-V2, FR-V3) now name their tests. No row deleted | `docs/requirements.md` |
| 10 | Skills | no repeatable check for "does the SRS match the code" | new skill `check-srs-vs-code` (report only), run once by hand for this log | skill file, this table |

## Found consistent, no change
- Project reference graph in `README.md` matches the csproj files (no cycles, everything ends in `Core`, `Messaging` is the only project with `RabbitMQ.Client`)
- Ports and option names in `README.md`, `docker.md`, `operation.md`, `compose.yaml` and the `Program.cs` / `*Settings.cs` files
- Contracts `modbus-registers.md`, `messaging.md`, `weather.md` against `RegisterMap`, `TelemetryMessage`, `WeatherApi` (field names and routing key)
- No secrets in the repo (`.env` ignored, `.env.example` has placeholders), `legacy/` untouched
- Error behavior table in the README: device down, broker down, invalid write, port in use all have a test or a manual run in the evidence

## Open from this check (not fixed in WP6)
| Item | Why | Where |
|---|---|---|
| `AGENTS.md` section "Projekt: EnergySim" does not list `Messaging`, `Gateway`, `Viewer`, the containers, the two test projects, build/test/run commands, team git rules | belongs to WP7 | `docs/todo.md` WP7 |
| Skill `update-development-journal` (Leon): branch rule only knows `teamarbeit`, our LB04 branches are `lb04/...` and the team journal is meant | not our skill to change without the owner | `docs/todo.md` |
| Skill table: `check-srs-vs-code` has no owner yet | team decision | `docs/todo.md` |
| Gateway polls devices one after another in one loop, a hanging device slows the others (known limit, already in the Gateway README) | design decision, no failing case yet | Gateway README |
| `TelemetryConsumer`, `TelemetryPublisher`, `GatewayRunner` (broker connection, reconnect) are not automated tested, only manual runs | needs a broker, would be an integration test with RabbitMQ in Docker, not required by LB04 | evidence of LB03 run, optional extension |
| LB03 acceptance criteria AC-1 to AC-9 stay `open` until the shared run in WP8 | evidence for the pause test is missing | WP8 |

## Not checked
- Windows and macOS
- Every project README line by line (only Cli, Gateway, Viewer, Messaging, Time.Cli, Modbus, WeatherService were opened because of WP4 to WP6 changes)
- Performance
