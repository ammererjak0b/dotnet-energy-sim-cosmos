# WP5 integration test: raw checks

State: branch `lb04/wp5-integration-test` on top of `323d509` (final commit hash is added in WP8). Host: Linux, .NET SDK 10. Automated tests, run by CLI. Not checked in an IDE.

## Run
`dotnet test tests/EnergySim.IntegrationTests --logger "console;verbosity=normal"`
```
Passed ...DeviceModbusServerTests.Read_BeforeFirstFrame_ShowsTypeAndMapVersionAndNoMasterSync [13 ms]
Passed ...DeviceModbusServerTests.Read_PublishedPvMidday_ReturnsDecodedHeaderAndDeviceBlock [7 ms]
Passed ...DeviceModbusServerTests.Write_ValidLimit_QueuesSetLimitCommandInPercent [2 ms]
Passed ...DeviceModbusServerTests.Write_LimitAboveRange_QueuesNothingAndSetsInvalidWriteFlagUntilNextValidWrite [23 ms]
Total tests: 4, Passed: 4
```
`--list-tests` lists the same 4. Full solution: `dotnet test EnergySim.sln`: unit 574 passed, integration 4 passed, build 0 warnings.

## Checks that the test can fail
- Changed the expected power from 3008 to 3009: test failed with `Expected: 3009, Actual: 3008`, the other 3 passed. Changed back.
- Failure path left no process or listening port behind (`ss -ltnp` shows no testhost).
- 8 repeated runs: 8 x passed (no flakiness seen).

## Limits
- Only the device Modbus server is tested over a real socket (PV profile). Other device profiles, clock endpoints, gateway, RabbitMQ are checked manually.
- Free-port search has a tiny race (port free at the check, taken at the start).
