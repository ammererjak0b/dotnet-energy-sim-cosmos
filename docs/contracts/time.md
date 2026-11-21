# Contract: simulation time

The time service (`EnergySim.Time.Cli`) is the master of the whole system. It is the only authority for virtual time. Devices never integrate their own time, they compute it from the last clock definition and their monotonic counter.

Full description: [Modbus and virtual-time architecture](../architecture/modbus-virtual-time.md). This file is the short contract.

## Controls (master only)
start, pause, resume, single step, restart with a new virtual start time, speed change. Each control takes effect at one common future instant (lead time), so all devices switch together. Pause, resume and speed change keep the virtual time continuous. Restart jumps and increments the generation.

**Single step:** only while paused (otherwise rejected). Virtual time jumps forward by `200 ms x speed` (the default device tick, fixed, also if a device runs another tick. Speed 60 = 12 s virtual, speed limit 3600 keeps it at 720 s), the clock stays paused. Sent as a normal clock definition (revision + 1, status Paused, new virtual anchor), generation unchanged. A device that sees the forward jump while paused runs exactly one model update with that step. A bigger forward jump while not running is ignored by the device (warning in the log). Only one change may be pending, a second step before the first is active (lead time, default 100 ms, `--lead-ms`) is rejected with "A clock change is already pending." Keys: `S` (dashboard), `step` (headless).

## Transport
Modbus TCP. Time service on port 1502. A device writes a registration into holding register 0 of the service, the service connects back, reads the device identity and pushes the clock definition and heartbeats.

**Registration (LB04):** FC16 write of 19 registers at address 0: register 0 = clock endpoint port, registers 1 to 18 = device identity (same layout as `ClockIdentityCodec`). The old form (FC06, 1 register = port only) is still accepted. Code: `ClockRegistrationCodec`.

**Hosts (default one machine, loopback):**
| Setting | Default | Container |
|---|---|---|
| Master bind address (`--host`) | `127.0.0.1` | `0.0.0.0` |
| Device clock endpoint bind address (`--clock-host`) | `127.0.0.1` | `0.0.0.0` |
| Device -> master host (`--time-host`) | `127.0.0.1` | `master` (service name) |
| Master -> device host | `127.0.0.1` | the announced device id (`--dial-device-id`), i.e. the compose service name |

Bind addresses must be an IP or `localhost`, `--time-host` is a host name or IP. Without `--dial-device-id` the master always dials loopback. Participants are keyed by host and port, so two containers can both use the default clock port. The master and a device log a warning when they listen on a non-loopback address, there is no authentication. With `--dial-device-id` the master connects to the announced host name, use it on a private network only.

`--autostart` (master): start the clock right after the service is up (unattended run).

## Clock definition (fields)
SessionId, Generation, Revision, CounterFrequency, AnchorTimestamp, VirtualAnchor, Speed, Status.
Virtual time = VirtualAnchor + (counter - AnchorTimestamp) / CounterFrequency * Speed while running, frozen while paused or stopped.

## Tolerance
Final tolerance: **50 ms virtual** between a device and the master timeline (master skew column), at speed up to 600. Measured (proof run, headless, 5 devices on one machine, loopback): 0 to -7 ms at speed 60, -2 to -7 ms at speed 600; earlier run up to -16 ms. Reason: sampling latency in microseconds times speed, the clock has no drift (no growth over time, also not after speed change, pause, resume, restart).
State timestamp in the header (input registers 9..12) is the time of the last computed state: two devices differ by at most one device tick, 200 ms real (measured 163 to 192 ms real at speed 60, 39 ms at speed 600). That difference jumps between two values, it does not grow. Evidence: [LB02 proof run](../evidence/README.md).

## New and unreachable simulators
- New device: registers at the service, listed as new, takes the current definition.
- No heartbeat acknowledged 3 times: marked unreachable, the master does not block. The device can register again.
- Master gone: the device keeps running on its last definition, shows a warning (warn flag b0, clock state Unsynced is only for devices that never synced), retries registration.
- Master restart: new session, devices reset to the new timeline.
