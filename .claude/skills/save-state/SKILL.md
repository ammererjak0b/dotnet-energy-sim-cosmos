---
name: save-state
description: Write a handover snapshot of the project (docs/project-state.md) so a new session can continue without the old chat. Use when the user asks to save state, checkpoint or hand over.
---

# save-state (Projektstand übergeben)

- **Owner:** Jakob
- **Source:** based on a "save-state" skill found online. Original author and license: TODO (Jakob, fill in before submission). Adapted for this repo: agent neutral, fixed target file, fixed sections, explicit invocation only, own limits
- **Purpose:** secure goal, done work, decisions, checks, open points and next step, so a new session can continue. Refers to existing docs instead of copying them
- **Invocation:** user asks explicitly, e.g. "save state" / "hand over". Not started on its own
- **Context:** `git status`, `git log -10`, `docs/development-journal/team.md`, `docs/SRS-LB03.md`, open diff. Nothing else
- **Output:** `docs/project-state.md`, overwritten each time (git keeps history)
- **Steps:**
  1. Read the context above, check the real state (build/test result only if it was run in this session)
  2. Write the file with these sections, keywords only, as short as possible:
     - Goal and branch
     - Done (since last save)
     - Decisions (with reason)
     - Checks done / not done (honest: "not verified" is allowed)
     - Open points, blockers (**OPEN** in bold)
     - Next step (one)
  3. Link to docs instead of repeating them (SRS, contracts, journal)
  4. Answer with one line: path and number of sections
- **Limits:**
  - writes only `docs/project-state.md`, no commit, no push
  - no secrets, no `.env` content, no passwords (write "ask the owner")
  - no invented work: only what is visible in git, files or this session
  - no code dumps, no line numbers
  - restoring context in a new session does not authorize any change
- **Proof of use:** TODO date, agent, result (path), own check: compare file with `git log` and the journal, note what was wrong
