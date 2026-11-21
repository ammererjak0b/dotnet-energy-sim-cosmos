# WP6 consolidation: raw checks

State: branch `lb04/wp6-consolidation` on top of `0f380a2` (final commit hash is added in WP8). Host: Linux, .NET SDK 10. Findings and changes: [[technical-documentation/consolidation-lb04]].

## Automated
`dotnet build EnergySim.sln`: 0 warnings, 0 errors.
`dotnet test EnergySim.sln`:
```
Passed!  Failed: 0, Passed: 9,   Total: 9    EnergySim.IntegrationTests.dll
Passed!  Failed: 0, Passed: 618, Total: 618  EnergySim.UnitTests.dll
```
New in WP6: 44 unit tests (`TelemetryMapperTests`, `GatewaySettingsTests`, `TelemetryJsonTests`, `RoutingTests`, `RabbitOptionsTests`, `TelemetryFormatTests`) and 5 integration tests (`GatewayDeviceTests`).

## Test can fail
Removed `weatherStale` from the quality rule in `TelemetryMapper`: `Map_WarningOrUnsyncedClock_IsUncertain(warnFlags: 2 ...)` failed, 8 others passed. File restored with `git checkout`, 9 of 9 passed again.

## Code reading only (not run)
- Project reference graph against the csproj files
- Contracts against `RegisterMap`, `TelemetryMessage`, `WeatherApi`
- Ports and option names in the docs against `Program.cs`, `*Settings.cs`, `compose.yaml`

## Limits
Containers and the full data flow were not rerun in WP6 (no change in that area). The consolidation log lists what was not checked.
