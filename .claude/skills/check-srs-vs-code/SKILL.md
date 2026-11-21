---
name: check-srs-vs-code
description: Check docs/requirements.md against the real repo: every status (implemented, verified, open) must be backed by code, a test or an evidence file. Reports mismatches, changes nothing without confirmation. Use before a merge, before submission or when docs and code may have drifted.
---

# check-srs-vs-code (SRS gegen Code prüfen)

- **Owner:** open (team decision, see `docs/todo.md`)
- **Purpose:** requirements, acceptance criteria, docs and code show the same state, no status without proof
- **Invocation:** user asks explicitly, e.g. "check srs vs code". Not started on its own. Claude: `/check-srs-vs-code`, Codex: `$check-srs-vs-code`, Copilot: "follow `.agents/skills/check-srs-vs-code/SKILL.md`"
- **Context:** `docs/requirements.md`, `docs/todo.md`, `docs/contracts/`, `docs/evidence/`, `src/`, `tests/`, `compose.yaml`, `scripts/`
- **Steps:**
  1. Report only, change nothing. Run `dotnet build EnergySim.sln` and `dotnet test EnergySim.sln` first, note warnings, failures and the real counts (`dotnet test <project> --list-tests`)
  2. For every row in `docs/requirements.md` with status `verified`: find the proof (test name, evidence file, log) and check it exists and passes. No proof found: list the row as "claimed without proof"
  3. For every row with `implemented` or `done`: find the code (class, option, file) with grep. Nothing found: list as "claimed, code missing"
  4. For every row with `open`: look for code or tests that already cover it: list as "maybe done, status outdated"
  5. Compare numbers in the docs (test counts, ports, option names, file paths) with the repo. List every difference. Options and ports: `README.md`, `docs/user-documentation/`, `docs/contracts/` against `src/**/Program.cs`, `*Settings.cs`, `compose.yaml`
  6. Check `docs/todo.md`: open items without requirement ID, requirement rows `open` without a to-do item, done items still in "Open"
  7. Answer with one table per finding type (requirement ID, claim, what was found, proposed change) and a short list of what is consistent. Ask which changes to apply
  8. After confirmation: change only the status text, numbers and paths named in the table. Add a journal line (`docs/development-journal/team.md`) with what was checked and what changed
- **Limits:**
  - never marks a requirement `verified` without a test run or evidence file the agent has seen
  - never deletes a requirement because it is not implemented (earlier stages stay traceable)
  - never invents results, owners or contributions
  - no commit, no push, no network, nothing outside the repo
  - the result must be read by a team member before it is relied on
- **Proof of use:** 2026-11-21, Claude Code (Sonnet 5.5), run by hand in LB04 WP6, findings in `docs/technical-documentation/consolidation-lb04.md`. Not yet read by a team member: TODO name and date
