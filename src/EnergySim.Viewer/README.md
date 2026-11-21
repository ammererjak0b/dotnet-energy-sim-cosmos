# EnergySim.Viewer

| | |
|---|---|
| What | RabbitMQ consumer, prints one text block per message, marks Bad/Uncertain. Reads no Modbus |
| Type | exe |
| Uses | `Messaging` |
| Used by | nobody (exe) |
| Owner | open |
| Start | `dotnet run --project src/EnergySim.Viewer` (needs `.env`) |
| Tests | `tests/EnergySim.UnitTests/Viewer` |
| Docs | `docs/contracts/messaging.md` |


Empfängt Telemetrie von RabbitMQ und druckt einen Block je Nachricht. Liest kein Modbus.

## Start
```bash
set -a; source .env; set +a   # RABBITMQ_USER, RABBITMQ_PASSWORD
dotnet run --project src/EnergySim.Viewer
dotnet run --project src/EnergySim.Viewer -- --Rabbit:Routing telemetry.pv.*
```
Beenden: Ctrl+C oder `quit`, Exit-Code 0.

## Verhalten
- Bindet `telemetry.#` (Standard) oder `--Rabbit:Routing`, eigene exclusive Queue, kein Backlog
- Zeigt DeviceId, `simTimeUtc`, `seq`, Quality, State und Messwerte mit Einheit. `Bad` und `Uncertain` markiert
- Broker weg: Meldung, Reconnect 1 s bis 10 s
- Ungültige Nachricht: Zeile, ignoriert

## Tests
`tests/EnergySim.UnitTests/Viewer/TelemetryFormatTests.cs`: text of a message (Good, Bad, Uncertain). The console output itself is a thin call in `ViewerRunner`.
