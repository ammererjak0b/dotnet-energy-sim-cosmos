# Technical documentation

Everything decided and done. Requirements: [[requirements]]. User view: [[user-documentation/index|User documentation]].

| Note | Content |
|---|---|
| [[technical-documentation/architecture\|Architecture]] | Processes, ports, projects, references, data flow |
| [[technical-documentation/decisions\|Decisions]] | What was decided and why, per phase |
| [[technical-documentation/test-and-evidence\|Test and evidence]] | Tests, proof runs, run scripts |
| [[technical-documentation/workflow\|Workflow]] | Team, agents, repo rules |

## Existing detail docs (not duplicated here)
- [[architecture/modbus-virtual-time]]: clock model, Modbus transport, startup, failures
- [[contracts/modbus-registers]], [[contracts/time]], [[contracts/weather]], [[contracts/messaging]]: contracts, change together with code
- [[rabbitmq]]: broker setup
- [[evidence/README|Evidence]]: LB02 proof run
- Journals: [[development-journal/team|team]], [[development-journal/jakob-ammerer|Jakob]], [[development-journal/jonas-kreibich|Jonas]], [[development-journal/Leon_Angerer|Leon]]
- Per project: `src/EnergySim.<Project>/README.md`
