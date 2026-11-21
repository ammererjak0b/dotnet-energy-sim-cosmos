# To-do LB04

Single work plan for the team and the teacher. One ticket = one work package (WP), one task branch, one PR into `main`.
Requirements behind each WP: [[requirements]]. Decisions: [[technical-documentation/decisions]].
Update this file in the same commit that finishes a ticket. Remove done items from "Open" and move the line to "Done".

Owners: J = Jakob, Jo = Jonas, L = Leon, all = all three (Discord call, one machine).

## Submission checklist (SUB-1 to SUB-4)
- [x] Source + `.NET 10` solution builds together (0 warnings)
- [x] Tests, commands, prerequisites: `tests/README.md`
- [x] Dockerfiles + `compose.yaml`, `.env.example`, RabbitMQ config
- [x] Central README, project READMEs, user and dev docs, SRS (`docs/requirements.md`), to-do (this file)
- [x] `AGENTS.md` / `CLAUDE.md`, skills in `.agents/skills` and `.claude/skills`
- [x] Evidence: `docs/evidence/lb04/`
- [ ] Each person: own journal in `docs/development-journal/<name>.md` (Done, KI, Artefact, accepted/changed/rejected AI suggestions)
- [ ] Each person: own container started once, id and connection settings shown (Leon master, Jakob weather, Jonas wind)
- [ ] PR merged into `main`, then `git tag -a lb04-2026 -m "LB04"` on the merged commit, `git push origin lb04-2026`, note the full hash here
- [ ] Read access for the teacher account on the remote
- [ ] Deadline in Moodle

## Open
| Prio | WP | Ticket | Owner | Requirements |
|---|---|---|---|---|

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
- Windows: someone runs `scripts/run-simulation.ps1` (native and docker) and `scripts/proof-run.ps1` once on a real Windows machine (only checked with PowerShell 7 on Linux: `proof-run.ps1` ran fully, `run-simulation.ps1` parsed and its window command checked)
- Portainer: first visit by hand (admin password), once on Windows Docker Desktop
- Team: compare the git rules in `AGENTS.md` with the Moodle guide "Git-Workflow im Team" (the agent did not have it)
- Leon: skill `update-development-journal` only knows branch `teamarbeit`, add the `lb04/...` branches (team journal)
- Team: owner of skill `check-srs-vs-code`, owners of `Messaging` and `Viewer`
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
| WP8 | Evidence index, tests, shared end-to-end run, AC-1 to AC-9 verified, requirement statuses, viewer/gateway stdin fix, PR | 2026-11-21 |
| Presentation guide | `docs/user-documentation/presentation.md`: setup, demo parts (native and Docker), tests, proof locations, problems | 2026-11-21 |
| Scripts | `run-simulation` (native, pv2, docker, stop) and `proof-run` for Linux and Windows, old `testrun_task2/3.sh` removed, Portainer in compose, short quick-facts table in every project README (17 + 2 test projects) | 2026-11-21 |
| Review fixes | WP5, WP6, WP7 review findings: header wait fails with a message, limit register read back, restored value after a rejected write, no stale values after an outage, shared `PvServerTestBase`, free-port retry, null guard in `TelemetryFormat`, all lambdas in test code removed (619 unit + 9 integration green) | 2026-11-21 |
| WP7 | `AGENTS.md`: structure, rules, commands, team git rules, doc map, LB04 test policy | 2026-11-21 |
| WP6 | Consolidation check: Gateway/Messaging/Viewer tests, viewer formatter extracted, style fixes, doc fixes, skill `check-srs-vs-code`, log [[technical-documentation/consolidation-lb04]] | 2026-11-21 |
| WP5 | Integration test project, 4 tests: real device Modbus server + real client (574 unit + 4 integration green) | 2026-11-21 |
| WP4 | Loopback lift, id as host name, 3 Dockerfiles, compose, `docker.md`, SIGTERM + headless stdin fix, review fixes, evidence (574 green) | 2026-11-21 |
| WP3 | Unit project renamed `EnergySim.UnitTests`, model coverage checked, PV irradiance tests added, test selection documented (545 green) | 2026-11-21 |
