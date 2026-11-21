# Tests

Two xUnit projects, both in `EnergySim.sln`. Both need only the .NET 10 SDK (`global.json`) and no internet after the package restore.

| Project | Kind | What | Needs |
|---|---|---|---|
| `tests/EnergySim.UnitTests` | unit | models, clock, loop, codecs, CLI, terminal. Fakes instead of network and clock | nothing running |
| `tests/EnergySim.IntegrationTests` | integration | real device Modbus server + real register client over TCP | a free local port (found by the test) |

Integration tests: `DeviceModbusServerTests` (register client against a real device server) and `GatewayDeviceTests` (gateway device against a real device server, no broker). Details of the unit tests: [EnergySim.UnitTests/README.md](EnergySim.UnitTests/README.md). Test selection and reasons: [docs/technical-documentation/test-and-evidence.md](../docs/technical-documentation/test-and-evidence.md).

## Commands (repo root)
```bash
dotnet build EnergySim.sln                                 # 0 warnings
dotnet test EnergySim.sln                                  # unit + integration
dotnet test tests/EnergySim.UnitTests                      # unit only
dotnet test tests/EnergySim.IntegrationTests               # integration only
dotnet test tests/EnergySim.IntegrationTests --list-tests  # lists the tests, 0 = nothing is checked
dotnet test tests/EnergySim.IntegrationTests --logger "console;verbosity=normal"   # test names + result
dotnet test EnergySim.sln --filter "Category=Integration"  # integration tests by label (also runs in the solution)
```
Expected: all passed, and `--list-tests` lists more than 0 tests per project (exact counts are in the evidence files, they change with every new test). IDE (Rider, Visual Studio): open `EnergySim.sln`, run the tests from the test explorer, the integration tests carry the label (trait) `Category=Integration`.

## Integration test: how it works and how to repeat it
File: `tests/EnergySim.IntegrationTests/DeviceModbusServerTests.cs`.

1. **Initial state:** per test a new `DeviceModbusServer` (device type PV) starts on a free port of `127.0.0.1` (the OS gives the port, no fixed port, so parallel runs and a running simulator do not clash). A real `PvDevice` with a fixed weather sample (800 W/m2, 20 degC) is updated once and published as a frame, like the simulation loop does.
2. **Server:** started by the test, nothing to start by hand. Start timeout 10 s.
3. **Real call:** the real `ModbusRegisterClient` (EasyModbus over TCP) reads the 17 header registers and the device block, and writes holding registers.
4. **Fixed expectations** (from the PV formula and `docs/contracts/modbus-registers.md`): power 3008 W, state Running, virtual time = published time, sequence 7, clock Running, no warning, device block Irradiance 800, ModuleTemperature 40.0 degC (raw 400), SunElevation 60.0 deg (raw 600). Write limit 500 permille arrives as command `SetLimit` 50 %. Write 1500 permille is rejected: no command, warn flag `InvalidWrite` until the next valid write.
5. **Timeouts:** server start 10 s, connect and read 2 s (client), wait for the effect of a write 5 s. A test cannot hang.
6. **Cleanup:** `IAsyncLifetime.DisposeAsync` closes client and server after every test, also after a failed assertion (checked: a deliberately failing expectation left no listening process).

Repeat: `dotnet test tests/EnergySim.IntegrationTests`. If a port clash happens (tiny race between "find free port" and server start) the test fails loudly, run again.

What is not covered: Modbus between master and devices (clock endpoints), gateway, RabbitMQ, containers. Those are checked by the manual runs in `docs/evidence/`.
