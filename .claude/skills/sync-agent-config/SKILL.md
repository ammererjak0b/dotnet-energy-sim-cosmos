---
name: sync-agent-config
description: Keep the agent setup of this repo in sync for Claude, Codex and Copilot. .agents/skills is the source, .claude/skills the copy, AGENTS.md the shared rules and skill table. Use after a skill or AGENTS.md was added, changed or renamed.
---

# sync-agent-config (Agent-Struktur abgleichen)

- **Owner:** Jakob
- **Purpose:** one source of truth for all agents, no drift between the copies
- **Invocation:** user asks explicitly, e.g. "sync agent config". Not started on its own. Claude: `/sync-agent-config`, Codex: `$sync-agent-config`, Copilot: "follow `.agents/skills/sync-agent-config/SKILL.md`"
- **Structure (rules):**
  - `.agents/skills/<name>/SKILL.md` = source, edit only here
  - `.claude/skills/<name>/SKILL.md` = exact copy for Claude Code
  - `AGENTS.md` = shared rules + table of all skills (Codex, Copilot read it)
  - `CLAUDE.md` = only `@AGENTS.md` (Claude reads it, no second rule set)
- **Context:** `.agents/skills/`, `.claude/skills/`, `AGENTS.md`, `CLAUDE.md`
- **Steps:**
  1. Report first, change nothing: diff `.agents/skills` against `.claude/skills` (`diff -r`), list folders without `SKILL.md`, rows of the `AGENTS.md` skill table without folder and folders without row, `CLAUDE.md` content
  2. If a copy differs: the newer file (`git log`, mtime) is the candidate. Both sides changed -> stop and ask, never overwrite
  3. After confirmation: copy `.agents` to `.claude` (source wins), add or fix table rows (name, owner from the `Owner:` line, one-line purpose)
  4. Check each `SKILL.md` has: `name` equal to folder name, `description`, owner, purpose, invocation, context, steps, limits. List gaps, do not fill them
  5. Answer: what was synced, what is still missing
- **Limits:**
  - touches only `.agents/skills/`, `.claude/skills/`, the skill table in `AGENTS.md`, `CLAUDE.md`
  - never edits skill content of other people, never writes `TODO` answers
  - no commit, no push, no deleting a skill folder without explicit confirmation
  - no secrets, nothing outside the repo
- **Open:** Copilot reads `AGENTS.md`. If the team wants a skills folder for Copilot too (`.github/skills`), verify the current Copilot docs first, then add it as a third copy here
- **Proof of use:** TODO date, agent, result, own check (e.g. `diff -r .agents/skills .claude/skills` empty afterwards)
