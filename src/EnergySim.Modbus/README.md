# EnergySim.Modbus

This project provides the loopback-only asynchronous integration boundary between device simulators and the time service. EasyModbus performs its synchronous Modbus TCP operations behind this boundary; received writes are handed to async consumers through channels.

The service endpoint accepts a device's callback port at holding register 0. Devices expose their identity as input registers 0–17 and, as an extension of protocol v1, a virtual time sample at input registers 18–27 (`ClockVirtualTimeCodec`: sequence, virtual unix ms, sampled counter, sequence again; refreshed every 200 ms, read by the master with each heartbeat for the skew; a block with differing sequence registers is a torn read and is dropped). They receive clock definitions at holding registers 0–31 and heartbeats at 32–47. Clock endpoints use unit id 1 and default ports 1601 pv, 1602 battery, 1603 charger, 1604 wind. Details: `docs/architecture/modbus-virtual-time.md`. The time service publishes its versioned timing policy at input registers 100–103. All endpoints bind to `127.0.0.1`.

Clock definitions include session, generation, revision, monotonic anchor, virtual UTC anchor, speed, and run status. Devices extrapolate from the monotonic anchor locally, so network traffic is limited to registration, state changes, and periodic health checks.

On heartbeat expiry, the device clock keeps advancing with its last definition and exposes an offline warning. `SharedClockConnection` retries registration while disconnected; authoritative initialization can replace an old service session or revision and clear a previous synchronization fault. Device clocks start heartbeat monitoring as soon as initialization arrives, including when the first heartbeat is never received. Healthy connections do not send registration requests. The service resends the current definition and any pending change when an already registered endpoint retries registration.

---

## Device Modbus server (`DeviceModbusServer`)

Separate from the clock endpoints above (default ports 1511 pv, 1512 battery, 1513 charger, 1514 wind, unit id configurable, default 1). One server per device process, contract: `docs/contracts/modbus-registers.md`.

**Classes:** `DeviceModbusOptions` (`validate()`, port 1502+), `DeviceModbusServer` (registers, lifecycle, write handling), `DeviceModbusSink : ISimulationSink` (frame -> registers), `DeviceModbusPortInUseException`.

**Wiring (Cli):**
- `await server.startAsync(ct)` before the loop, `await server.stopAsync(ct)` on shutdown
- pass `new DeviceModbusSink(server)` to the loop (e.g. in the FanOutSink)
- `Task forward = server.runCommandsAsync(commandChannel.Writer, ct)` forwards valid writes as `DeviceCommand` into the loop's command channel
- `DeviceModbusPortInUseException` on start -> exit code 1

**Sync library boundary:** EasyModbus is synchronous. `Listen()`/`StopListening()` are called directly, no `Task.Run`, no `.Wait()`. Its `HoldingRegistersChanged` event runs on a library thread: it only validates, restores registers and writes into a `Channel`. The simulation is never touched there. `startAsync` waits with `ConnectAsync` + `Task.Delay` until the port really accepts connections.

**Experiments (EasyModbusTCP.NETCore 1.0.0):**
- Unit ID: enforced. Request with another unit ID gets no answer (client timeout). Unit ID 0 is answered (broadcast). Configure `Modbus:UnitId` on both sides.
- Server arrays are `short[]`, 1-based. `HoldingRegistersChanged(start, count)`: `start` is 1-based (client address + 1), the written values are already in `holdingRegisters.localArray[start + i]`. FC06 and FC16 both fire the event.
- Port in use: `Listen()` does not throw. The bind error is thrown on the library thread and kills the process. So `startAsync` probes the port with a `TcpListener` first (tiny race until `Listen()` binds).
- Thread safety: library reads take no lock for input registers. Updates run under our lock (against the write handler only). A read that overlaps an update can mix two states (stress test with updates in a tight loop: ~2% of header reads torn). Normal tick rate: practically never. Clients that care read the sequence before and after.
- Loop publishes every tick, also while paused: registers stay readable, sequence constant (it only grows on a device update or accepted command).

**Behavior:**
- Warn flags: b0 = clock not synced or using fallback, b1 = `frame.WeatherStale`, b2 = invalid write (cleared by the next valid write)
- Clock state register: Unsynced when neither synced nor fallback, else Running/Paused/Stopped from the snapshot
- Holding registers are initialised from the device header on the first frame only. Later device changes (keyboard, reset) are not mirrored.
- Limit register: permille, command value = permille / 10 (percent)
- Fault reset register: back to 0 by the server right away, command only sent when 1
