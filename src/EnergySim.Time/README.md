# EnergySim.Time

`TimeAuthority` owns the shared virtual clock timeline. Controls are scheduled for a common future monotonic instant so connected devices can apply the same revision consistently. `ClockServiceHost` coordinates local Modbus participants, sends definitions and heartbeats, and removes endpoints that remain unreachable so they can register again.

`TimeServiceOptions` configures the local TCP port, scheduling lead, heartbeat interval, expiry policy, initial virtual UTC time, and speed. `EnergySim.Time.Cli` is the master simulator of the system: the single authority for virtual time.

Registered devices continue with their last clock settings if the service stops, and reconnect automatically when it returns. The host answers repeated registrations with its active definition and any pending transition. A restarted host owns a new session: devices adopt it and reset their model onto its timeline, even if its generation number matches the old service's generation. The standalone service starts stopped and requires its Start command before shared time advances again.

## Reuse

`EnergySim.Time.Cli` is the master. Another host can create `TimeAuthority` and `ClockServiceHost` directly and own their startup, shutdown and control commands. It should not invoke `TimeServiceRunner`, which parses command-line options and creates a separate authority instance.

The generic `DeviceDashboard` in `EnergySim.Terminal` is reusable by both applications. `TimeServiceDashboardSource` currently lives in the standalone CLI; another host can provide its own `IDashboardSource` or move that adapter into a shared UI project if it wants the same clock screen. The current device `CliRunner` is an executable composition root, not a device-launch API.

## Participants and failure handling

Each registered simulator has a status. `NEW`: registered, no heartbeat delivered yet. `OK`: last heartbeat delivered. `UNREACHABLE`: 3 heartbeats in a row failed; it stays listed with last seen time and is retried every heartbeat.

Skew: devices publish their own virtual time and the monotonic counter at sampling in clock endpoint input registers 18..27 (`ClockVirtualTimeCodec`, backward compatible, old readers ignore it). The master reads it with every heartbeat and shows device minus master virtual time. The read is also the liveness proof, because EasyModbus writes to a dead peer do not fail. Residual risk: registers 18..27 are not read atomically, a torn read inside the value is not detected (the sequence check covers only the ends).

| Case | Behavior |
| --- | --- |
| Simulator unreachable | log line, status UNREACHABLE, retried; back to OK after it registers again or answers |
| Master restart | new session; devices keep running on their last clock, register again and adopt it, status NEW then OK |
| Duplicate device id | second port rejected, logged once; an UNREACHABLE holder of the id is replaced |
| Registration port below 1502 or above 65535 | rejected, logged |
| Registration port equal to the service port | rejected, logged |
| Registered port answers with another device ID | old entry replaced (or rejected as duplicate) |
| Service port in use | `SocketException` or `DeviceModbusPortInUseException` from start, the CLI prints a message and exits with code 1 |
