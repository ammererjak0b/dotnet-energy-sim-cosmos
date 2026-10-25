# EnergySim.Time

`TimeAuthority` owns the shared virtual clock timeline. Controls are scheduled for a common future monotonic instant so connected devices can apply the same revision consistently. `ClockServiceHost` coordinates local Modbus participants, sends definitions and heartbeats, reads device status, and removes endpoints that remain unreachable so they can register again.

`TimeServiceOptions` configures the local TCP port, scheduling lead, heartbeat interval, expiry policy, initial virtual UTC time, and speed. The service is intended to run independently of a future master simulator; a master can later coordinate it through the authority API.
