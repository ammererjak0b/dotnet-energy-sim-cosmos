# Workflow

## Team
| Person | Owns |
|---|---|
| Jakob | PV, WeatherSim, `Cli`, `Diagnostic` |
| Jonas | Battery, Wind, Modbus device layer |
| Leon | TimeSim, Charger, `Time`, `Time.Cli`, `Terminal`, `Gateway` |
| open | `Messaging`, `Viewer` |

## Rules (`AGENTS.md`, sections "Projekt: EnergySim" and "Git im Team")
- Naming, no inline ifs, no syntactic sugar, keyword comments, one class per file
- LB02 basis: TDD (red, green, refactor). Integration, LB03 and LB04: no test-first, tests written after the code
- Definition of done: 0 warnings, all tests pass, touched README up to date
- Repo hygiene: no scratch files, no `_v2`/`_final`, no secrets, generated files ignored
- `legacy/` never changed
- Git: branch per ticket, small commits, PR into `main` after build, tests and diff are checked, never overwrite or discard unfinished work of others. Full list in `AGENTS.md` section "Git im Team"

## Branches
`main`, `teamarbeit` (LB02 integration, merged via PR #2), `lb03`, then one branch per LB04 ticket: `lb04/wp1-docs` (merged via PR #3), `lb04/wp2-step`, `lb04/wp3-tests`, `lb04/wp4-containers`, `lb04/wp5-integration-test`, `lb04/wp6-consolidation`. Each branch builds on the previous one, one push and PR at the end.

## Agent skills (`.agents/skills`, copy in `.claude/skills`)
| Skill | Purpose |
|---|---|
| `save-state` | handover snapshot `docs/project-state.md` |
| `update-proof-run-scripts` | keep proof-run scripts in sync |
| `sync-agent-config` | keep `.agents`, `.claude`, `AGENTS.md` in sync |
| `check-srs-vs-code` | report requirement statuses without proof, no changes without confirmation |
| `i-have-adhd` | output style (external skill, only on request) |
| `update-development-journal` | write the next journal entry from git changes |
