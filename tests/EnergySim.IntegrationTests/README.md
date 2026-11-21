# EnergySim.IntegrationTests

| | |
|---|---|
| What | integration tests: real device Modbus server (PV model, free local port) with the real register client and with the gateway device. No broker |
| Type | xUnit test project, label `Category=Integration` |
| Uses | `Core`, `Modbus`, `Pv`, `Messaging`, `Gateway` |
| Run | `dotnet test tests/EnergySim.IntegrationTests` |
| Owner | shared |
| Docs | `tests/README.md` (how it works, how to repeat), `docs/evidence/lb04/wp5-integration-test.md` |

## Files
- `PvServerHarness`: starts the server on a free port with a fixed PV state, retries a taken port
- `PvServerTestBase`: start and stop around every test
- `DeviceModbusServerTests`: header, device block, limit write, rejected write
- `GatewayDeviceTests`: new state, same state, new sequence, wrong type, stopped device
