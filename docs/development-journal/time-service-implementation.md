# Development journal: shared time service

## 2026-10-25

- Implemented the shared monotonic clock definition and device-side scheduled clock in `EnergySim.Core`, including revision/session checks, pause-safe advancement, heartbeat expiry, and timeline-aware device loop updates.
- Added the `EnergySim.Modbus` transport, versioned codecs, loopback endpoints, device registration, and service timing policy block. Modbus networking is isolated behind async channel consumers.
- Added `EnergySim.Time` authority/host and `EnergySim.Time.Cli` dashboard. Clock actions are scheduled with a configured lead, and registered devices receive updates and periodic health checks.
- Added optional `--clock shared` wiring to the existing device CLI while keeping its default local clock behavior.
- Added deterministic tests for clock definitions, scheduling, safety, codecs, CLI options, and simulation-loop behavior. Adjusted CSV test file sharing for Windows compatibility.
- Fixed a scheduling race where a heartbeat for a future revision could arrive before its clock definition. Devices now ignore heartbeats that do not match the installed revision.
- Removed the extra status-register acknowledgement protocol and its polling. It produced torn-read errors and was unnecessary for clock synchronization. Reduced retry logs to service state changes.
- Simplified clock plumbing by removing unused cursor/session and transition-kind state, removing a duplicate device-side expiry option that was always overridden by the service, and ensuring all device receiver tasks are awaited together during shutdown.
- Documented how a future master can host the time service directly. Added a text parameter to the shared dashboard and made `R` prompt for a local-time reset timestamp in `yyyy-MM-dd HH:mm` format.
- Assistant: OpenAI Codex. Verification status: full solution build succeeded with 0 warnings and 0 errors; all 378 tests passed. A local service/PV smoke run stayed synchronized and applied a start command without heartbeat expiry. A follow-up complexity review removed unused clock fields/options and obsolete transition bookkeeping; build and all tests passed again.
