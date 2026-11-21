# Automated tests

State: branch `lb04/wp8-evidence`, Linux, .NET SDK 10. CLI only, not run in an IDE.

```
dotnet build EnergySim.sln          0 warnings, 0 errors
dotnet test EnergySim.sln
  Passed!  Failed: 0, Passed: 9,   Total: 9     EnergySim.IntegrationTests.dll
  Passed!  Failed: 0, Passed: 619, Total: 619   EnergySim.UnitTests.dll
dotnet test tests/EnergySim.UnitTests --list-tests          604 listed (theory rows are listed once)
dotnet test tests/EnergySim.IntegrationTests --list-tests   9 listed
```

## Integration tests executed (real Modbus over TCP)
`DeviceModbusServerTests` (4): header before the first frame, published PV state (3008 W, device block 800 / 400 / 600), valid limit write (command `SetLimit` 50 %, register keeps 500), write above range (no command, register restored to 1000, flag `InvalidWrite` until the next valid write).
`GatewayDeviceTests` (5): new state, same state (nothing new), new sequence, wrong type (one `Bad`), stopped device (one `Bad`, no values, no sim time).
How to repeat: [tests/README.md](../../../tests/README.md). Test selection, expectations and prerequisites: [[technical-documentation/test-and-evidence]].

## Test checks that a test can fail
- Changed expected power 3008 to 3009: integration test failed, changed back (WP5)
- Removed `weatherStale` from the quality rule: `TelemetryMapperTests` failed, restored (WP6)
- Removed the null guard in `TelemetryFormat`: `Format_ListsAreNullInJson_DoesNotThrow` failed with `ArgumentNullException`, restored
- Removed the `Task.Run` in `HeadlessRunner`: `RunAsync_CancelWhileStdinBlocked_Returns` hung (does not finish), restored

## Not covered
- IDE run (Rider/Visual Studio), Windows, macOS
- Broker publisher/consumer/reconnect: manual run in [[evidence/lb04/e2e-native]]
- `Diagnostic` client, Dockerfiles, compose, run scripts: manual checks
