# EnergySim.Gateway

Modbus-Client und Publisher: liest alle Geräte, dekodiert Header und Gerätewerte, veröffentlicht neue Stände an RabbitMQ.

## Start
```bash
set -a; source .env; set +a   # RABBITMQ_USER, RABBITMQ_PASSWORD
dotnet run --project src/EnergySim.Gateway
dotnet run --project src/EnergySim.Gateway -- --PollIntervalMs 500 --Rabbit:Host localhost
```
Beenden: Ctrl+C oder `quit`, Exit-Code 0. Fehlerhafte Argumente: Text, Usage, Exit-Code 2.

## Konfiguration (`appsettings.json`, überschreibbar per `--Key value`)
| Key | Standard |
|---|---|
| `PollIntervalMs` | 1000 (100 bis 60000) |
| `UnitId` | 1 |
| `Devices[]` | `Id`, `Type`, `Host`, `Port`: pv-1 1511, battery-1 1512, charger-1 1513, wind-1 1514 |
| `Rabbit:Host/Port/VirtualHost/Exchange` | localhost, 5672, `/`, `energysim.telemetry` |

## Verhalten
- Publiziert nur bei neuem Stand (`Sequence`, `Generation`, Clock-State, State, Fault, Warnungen). Pause: einmal beim Wechsel, danach nichts
- Torn Read: Sequenz wird nach dem Lesen der Gerätewerte erneut geprüft, bei Abweichung wartet das Gateway auf den nächsten Durchlauf
- Gerät weg: einmal Fehlerzeile + `Bad`, Retry je Intervall, danach wieder `Good`
- Broker weg: Retry 1 s bis 10 s, Zeile je Versuch, nicht gesendete Stände gezählt, nach Reconnect aktueller Stand
- `TelemetryMapper`: reine Funktion Header + Gerätewerte -> Nachricht, Einheiten und Skalen aus `DeviceProfiles`

## Grenzen
- Header und Gerätewerte in getrennten Anfragen, Torn Read wird erkannt, nicht verhindert
- Alle Geräte nacheinander in einer Schleife: Broker-Reconnect (bis 5 s) und hängende Geräte bremsen den Durchlauf
- Modbus-Aufrufe synchron (EasyModbus), ein hängendes Gerät bremst den Durchlauf bis zum Timeout
- Nichts wird gepuffert
