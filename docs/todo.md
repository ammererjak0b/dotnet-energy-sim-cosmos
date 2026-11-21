# To-do LB04

Single work plan for the team and the teacher. One ticket = one work package (WP), one task branch, one PR into `main`.
Requirements behind each WP: [[requirements]]. Decisions: [[technical-documentation/decisions]].
Update this file in the same commit that finishes a ticket. Remove done items from "Open" and move the line to "Done".

Owners: J = Jakob, Jo = Jonas, L = Leon, all = all three (Discord call, one machine).

## Open
| Prio | WP | Ticket | Owner | Requirements |
|---|---|---|---|---|
| 7 | WP8 | Evidence, journals, member mapping, tag `lb04-2026`, teacher access | all | EVD-1 to EVD-6, SUB-1 to SUB-4 |

## Open decisions
| Decision | State |
|---|---|
| Owners of `Messaging` and `Viewer` | open |
| To-do tool | decided: this file, in the repo |
| Gateway container | decided: no, gateway runs native and reaches containers via published ports |
| Callback host for containers | decided: master dials the device id as hostname |
| Single step meaning | decided: one device tick (200 ms real) at current speed, then paused |
| Test-first | decided: no, tests are written after the code, all green before merge |

## Open for later WPs
- Team: compare the git rules in `AGENTS.md` with the Moodle guide "Git-Workflow im Team" (the agent did not have it)
- Team decision: lambdas in test code allowed? (the Definition of done says no forbidden syntax, 36 test lines and a few Wind lines use lambdas)
- WP6 review findings not applied yet: stopped-device test should assert no measurements, FR-G6/MOD-5 wording, duplicated `getHarness()` in the two integration classes, free-port retry in `PvServerHarness`, null guard for missing lists in `TelemetryFormat`
- Leon: skill `update-development-journal` only knows branch `teamarbeit`, add the `lb04/...` branches (team journal)
- Team: owner of skill `check-srs-vs-code`, owners of `Messaging` and `Viewer`
- WP5 review nits not applied yet: silent timeout in the header wait helper, valid-write test does not read the limit register back, unused fourth block value
- WP8: per-person proof (each person builds and starts their container on their own, shows id and connection settings), second device in a container optional
- WP4 follow-up: test the compose setup on Windows/macOS Docker Desktop (not checked), native devices cannot join a master that dials by id
- Two wind containers (host:port key) are covered by a unit test of the key only, not run in Docker
- Check the unit tests in Rider/Visual Studio once (TST-6)

## Open from earlier stages
- LB03: pause test, evidence screenshots, acceptance criteria AC-1 to AC-9 still `open` (re-checked in WP8)
- Optional: time and weather over RabbitMQ

## Done
| WP | Ticket | Date |
|---|---|---|
| WP1 | LB04 requirements in `docs/requirements.md`, this to-do list, decisions logged | 2026-11-21 |
| WP2 | Master single step (`S`, `step`), contract + docs, tests (539 green) | 2026-11-21 |
| WP7 | `AGENTS.md`: structure, rules, commands, team git rules, doc map, LB04 test policy | 2026-11-21 |
| WP6 | Consolidation check: Gateway/Messaging/Viewer tests, viewer formatter extracted, style fixes, doc fixes, skill `check-srs-vs-code`, log [[technical-documentation/consolidation-lb04]] | 2026-11-21 |
| WP5 | Integration test project, 4 tests: real device Modbus server + real client (574 unit + 4 integration green) | 2026-11-21 |
| WP4 | Loopback lift, id as host name, 3 Dockerfiles, compose, `docker.md`, SIGTERM + headless stdin fix, review fixes, evidence (574 green) | 2026-11-21 |
| WP3 | Unit project renamed `EnergySim.UnitTests`, model coverage checked, PV irradiance tests added, test selection documented (545 green) | 2026-11-21 |
