---
name: update-proof-run-scripts
description: Convert a specified EnergySim Bash test-run script to its matching PowerShell script while keeping both aligned with the project behavior.
---

# Convert a specified EnergySim test-run script

Use this skill when an EnergySim executable, service, device, startup option, port, dependency, or proof step changes and the corresponding run scripts need updating.

## Required input

The caller must provide the repository-relative path to the `.sh` file to convert, for example:

`$update-proof-run-scripts scripts/testrun_task3.sh`

Never choose a script by searching for the highest number, by guessing from recent changes, or by falling back to another script. If the caller omits the path, ask which `.sh` file to convert before editing anything.

Accept a path only when it points to an existing Bash script under `scripts/`. Update that exact `.sh` file and its same-basename `.ps1` counterpart. For example, `scripts/testrun_task3.sh` maps to `scripts/testrun_task3.ps1`. If the requested `.sh` file does not exist or is outside `scripts/`, report the problem and ask for a corrected path. Create the matching `.ps1` if it does not exist. Do not modify other numbered scripts or `proof-run.*` as substitutes.

## Review relevant project behavior

Read repository `AGENTS.md`, the specified Bash script, its PowerShell counterpart if present, any scripts it invokes, and the relevant project documentation and configuration. This can include `README.md`, project READMEs, contracts, `compose.yaml`, and `.env.example`. Check Git status and diff to preserve unrelated work. Read credentials only to understand variable names and handling; never print or copy their values into output or tracked files.

## Keep the pair aligned

Update the specified Bash and matching PowerShell scripts together. They must start the same intended programs in dependency order, pass equivalent options and ports, perform the same proof actions, collect equivalent evidence, and shut down processes and supporting services consistently. Use each operating system's suitable process and input mechanisms while preserving observable behavior. Keep usage, build prerequisites, logging, and stop behavior aligned with current project conventions.

After editing, review the two specified scripts side by side and inspect the diff. Run available syntax checks for each script. Do not run the full multi-process run unless the user explicitly asks; it starts services and may affect local Docker containers. Do not add or alter credentials, commit, or push.

## Invocation

`$update-proof-run-scripts <path-to-script.sh>`