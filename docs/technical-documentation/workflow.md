# Workflow

## Team
| Person | Owns |
|---|---|
| Jakob | PV, WeatherSim, `Cli`, `Diagnostic` |
| Jonas | Battery, Wind, Modbus device layer |
| Leon | TimeSim, Charger, `Time`, `Time.Cli`, `Terminal`, `Gateway` |
| open | `Messaging`, `Viewer` |

## Rules (`AGENTS.md`)
- Naming, no inline ifs, no syntactic sugar, keyword comments, one class per file
- LB02 basis: TDD (red, green, refactor). Integration and LB03: no test-first
- Definition of done: 0 warnings, all tests pass, touched README up to date
- Repo hygiene: no scratch files, no `_v2`/`_final`, no secrets, generated files ignored
- `legacy/` never changed

## Branches
`main`, `teamarbeit` (LB02 integration, merged via PR #2), `lb03` (current).

## Agent skills (`.agents/skills`, copy in `.claude/skills`)
| Skill | Purpose |
|---|---|
| `save-state` | handover snapshot `docs/project-state.md` |
| `update-proof-run-scripts` | keep proof-run scripts in sync |
| `sync-agent-config` | keep `.agents`, `.claude`, `AGENTS.md` in sync |
| `update-development-journal` | write the next journal entry from git changes |
