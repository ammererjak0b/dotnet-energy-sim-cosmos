# Contract: simulation time

The time service (`EnergySim.Time.Cli`) is the master of the whole system. It is the only authority for virtual time. Devices never integrate their own time, they compute it from the last clock definition and their monotonic counter.

Full description: [Modbus and virtual-time architecture](../architecture/modbus-virtual-time.md). This file is the short contract.

## Controls (master only)
start, pause, resume, restart with a new virtual start time, speed change. Each control takes effect at one common future instant (lead time), so all devices switch together. Pause, resume and speed change keep the virtual time continuous. Only restart jumps, and it increments the generation.

## Transport
Modbus TCP, loopback. Time service on port 1502. A device writes its clock endpoint port into the service, the service connects back, reads the device identity and pushes the clock definition and heartbeats.

## Clock definition (fields)
SessionId, Generation, Revision, CounterFrequency, AnchorTimestamp, VirtualAnchor, Speed, Status.
Virtual time = VirtualAnchor + (counter - AnchorTimestamp) / CounterFrequency * Speed while running, frozen while paused or stopped.

## Tolerance
Allowed difference between two devices and the master: measured with the master headless and battery plus charger on one machine (device virtual time read from clock endpoint input registers 18..27, compared with the master timeline at the device sample instant). Speed 60: 0 to -7 ms. Speed 600: 0 to -14 ms, mostly 0 to -8 ms. The error is the microsecond gap between virtual time and counter sampling on the device, times speed, so it is a measurement floor, not drift. Proposed tolerance: 50 ms at speed up to 600.

## New and unreachable simulators
- New device: registers at the service, listed as new, takes the current definition.
- No heartbeat acknowledged 3 times: marked unreachable, the master does not block. The device can register again.
- Master gone: the device keeps running on its last definition, shows a warning (warn flag b0, clock state Unsynced is only for devices that never synced), retries registration.
- Master restart: new session, devices reset to the new timeline.
