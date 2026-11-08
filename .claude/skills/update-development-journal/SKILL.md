---
name: update-development-journal
description: Update the EnergySim development journal from the current Git changes, choosing the team or personal journal from the branch and Git identity.
---

# Update the EnergySim development journal

Use this skill after implementation work in this repository when the development journal should reflect the current changes.

## Choose the journal

Work from the repository root. Check the current branch, Git author identity, status, and diffs before editing.

- If the branch name contains `teamarbeit` (case-insensitive), update `docs/development-journal/team.md`. This rule takes precedence over personal identity.
- Otherwise, read the configured Git author name with `git config user.name`. Match it case-insensitively against these identities and update the corresponding file:
  - `LeonAngerer` → `docs/development-journal/Leon_Angerer.md`
  - `ammererjak0b` → `docs/development-journal/jakob-ammerer.md`
  - `KreibichJohnny` → `docs/development-journal/jonas-kreibich.md`
- If the configured name does not match, check `git config user.email` and repository commit authors for one of these same identities. If the author still cannot be identified, ask which personal journal to use; do not guess or create a new journal.

Use the existing file names exactly as they appear in `docs/development-journal/`.

## Review the changes

Inspect `git status --short --branch`, the current branch, and all relevant changes, including staged and unstaged diffs (`git diff` and `git diff --cached`). Include untracked files in the review by reading their contents when they are part of the implementation. Do not treat generated output, unrelated local files, or old changes as work from this update. If the changes are ambiguous, use recent commit context where helpful, and describe only what the evidence supports.

## Write the entry

Add a dated entry at the top of the selected journal, below its title and any introductory notes. Follow that file's language, date format, heading style, and level of detail. Read nearby entries first. Summarize the actual outcome in `Done` and list meaningful paths or artifacts in `Artefact` (or the file's established spelling). Include `KI` with the tool/model and how it was used only when known from the conversation or project evidence; never invent AI usage. Add `Test` only for tests that were actually run, and report outcomes accurately. For a team entry, include `Beteiligte` only when the participants are known.

Keep the note concise and useful as a development record. Do not rewrite historical entries, alter other journals, or claim unverified work. After editing, inspect the diff to confirm only the intended journal file changed and that the entry matches the code changes.

## TODO (Leon, fehlt für die Abgabe)

- **Owner:** Leon
- **Aufruf:** TODO Claude `/update-development-journal`, Codex `$update-development-journal`, Copilot "folge dieser SKILL.md"
- **Grenzen:** TODO kein Commit/Push, nur die gewählte Journaldatei
- **Quelle/Lizenz:** TODO falls übernommen
- **Nachweis:** TODO Datum, Agent, Ergebnis, eigene Prüfung
