# EnergySim.Modbus

This project provides the loopback-only asynchronous integration boundary between device simulators and the time service. EasyModbus performs its synchronous Modbus TCP operations behind this boundary; received writes are handed to async consumers through channels.

The service endpoint accepts a device's callback port at holding register 0. Devices expose their identity as input registers and receive clock definitions at holding registers 0–31 and heartbeats at 32–47. The time service publishes its versioned timing policy at input registers 100–103. All endpoints bind to `127.0.0.1`.

Clock definitions include session, generation, revision, monotonic anchor, virtual UTC anchor, speed, and run status. Devices extrapolate from the monotonic anchor locally, so network traffic is limited to registration, state changes, and periodic health checks.
