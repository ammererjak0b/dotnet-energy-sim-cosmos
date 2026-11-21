# To-do LB04

Single work plan for the team and the teacher. One ticket = one work package (WP), one task branch, one PR into `main`.
Requirements behind each WP: [[requirements]]. Decisions: [[technical-documentation/decisions]].
Update this file in the same commit that finishes a ticket. Remove done items from "Open" and move the line to "Done".

Owners: J = Jakob, Jo = Jonas, L = Leon, all = all three (Discord call, one machine).

## Open
| Prio | WP | Ticket | Owner | Requirements |
|---|---|---|---|---|
| 2 | WP3 | Split test projects (unit, integration), model tests (PV, battery, charger, wind), justify selection | all | TST-1, TST-2, TST-4 to TST-7 |
| 3 | WP4 | Lift loopback limit, master dials device id as hostname, Dockerfiles, compose, `docker.md` | J weather, Jo wind, L master | TIM-13, TIM-14, CTR-1 to CTR-11 |
| 4 | WP5 | Integration test: real device Modbus server + real client, fixed expectation, timeouts, cleanup | all | INT-1 to INT-6 |
| 5 | WP6 | Consolidation check (problems, fixes, checks), README and doc update, SRS check skill | all | CON-1 to CON-3, CON-7, CON-8 |
| 6 | WP7 | `AGENTS.md` / `CLAUDE.md`: structure, commands, team git rules | all | CON-6, GIT-1 to GIT-4 |
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

## Open from earlier stages
- LB03: pause test, evidence screenshots, acceptance criteria AC-1 to AC-9 still `open` (re-checked in WP8)
- Optional: time and weather over RabbitMQ

## Done
| WP | Ticket | Date |
|---|---|---|
| WP1 | LB04 requirements in `docs/requirements.md`, this to-do list, decisions logged | 2026-11-21 |
| WP2 | Master single step (`S`, `step`), contract + docs, tests (538 green) | 2026-11-21 |
