# EnergySim.Viewer

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
