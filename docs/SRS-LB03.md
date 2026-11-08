# SRS LB03: Modbus zu RabbitMQ

## Ziel und Umfang
- Gerätedaten per Modbus lesen, über RabbitMQ an einen Viewer liefern
- Neu: Messaging, Gateway, Viewer, Docker-Compose für RabbitMQ
- Bleibt wie in LB02: TimeSim, WeatherSim, Gerätesimulatoren, Diagnostic-Client
- Nicht im Umfang: InfluxDB, REST, Grafana, TimeSim/WeatherSim über RabbitMQ (optional, offen)

## Datenweg
```mermaid
flowchart LR
  D[Device Modbus-Server<br/>pv battery charger wind] -->|Modbus TCP| G[Gateway]
  G -->|AMQP 0-9-1| R[(RabbitMQ Docker)]
  R --> V[Viewer]
```

## Apps
| App | Aufgabe | Owner |
|---|---|---|
| Messaging (lib) | Nachricht, Routing, AMQP-Wrapper | offen |
| Gateway | Modbus lesen, dekodieren, publizieren | Leon |
| Viewer | empfangen, anzeigen, liest kein Modbus | offen |

## Entscheidungen
- AMQP 0-9-1 (`RabbitMQ.Client` 7.x): Exchange/Routing/Binding passt zur Aufgabe, async, kein Plugin. MQTT und AMQP 1.0 verworfen (AMQP 1.0 erzwingt persistente Nachrichten)
- Ein Gateway-Prozess für alle Geräte: einfach, Fehler pro Gerät getrennt behandelt
- Broker: `rabbitmq:4.3.7-management`, Zugangsdaten nur über `.env`

## Anforderungen Gateway
- FR-G1: Geräte über Id, Type, Host, Port konfigurierbar. Meldet ein Gerät einen anderen Typ: Fehler, `Bad`
- FR-G2: Header (17 Register) und Gerätewerte lesen, nach Registerplan dekodieren (`docs/contracts/modbus-registers.md`)
- FR-G3: Pollintervall konfigurierbar
- FR-G4: `simTimeUtc` aus Register, `polledAtUtc` = Abfragezeit, getrennte Felder
- FR-G5: nur publizieren, wenn sich `Sequence`, `Generation`, `ClockState`, `State`, Fault oder Warnungen ändern. Sequenz vor und nach dem Lesen vergleichen (Torn Read)
- FR-G6: Gerätefehler: einmal pro Ausfall Meldung + Nachricht `Bad`, keine alten Werte
- FR-G7: Brokerausfall: Reconnect mit Backoff, Meldung pro Versuch, nicht gesendete Stände zählen
- FR-G8: sauberes Beenden (Ctrl+C oder `quit`)

## Anforderungen Viewer
- FR-V1: bindet `telemetry.#`, Filter per `--Rabbit:Routing`
- FR-V2: zeigt DeviceId, `simTimeUtc`, `sequence`, `quality`, `state`, Messwerte mit Einheit
- FR-V3: `Bad` und `Uncertain` sichtbar markiert
- FR-V4: Reconnect bei Brokerausfall, sauberes Beenden

## Nachricht und Routing
Siehe `docs/contracts/messaging.md`.

## Konfiguration
| Key | Default | Bedeutung |
|---|---|---|
| `PollIntervalMs` | 1000 | Abfrageintervall |
| `UnitId` | 1 | Modbus Unit |
| `Devices[]` | pv-1, battery-1, charger-1, wind-1 | `Id`, `Type` (Pv, Battery, Charger, Wind), `Host`, `Port` (1511 bis 1514) |
| `Rabbit:Host/Port/VirtualHost/Exchange` | localhost, 5672, `/`, `energysim.telemetry` | Broker |
| `RABBITMQ_USER`, `RABBITMQ_PASSWORD` | aus `.env` | Zugangsdaten, nicht im Repo |

## Fehlerverhalten
| Fall | Verhalten |
|---|---|
| Gerät nicht erreichbar | Konsolenfehler, einmal `Bad`, Retry je Intervall, danach wieder `Good` |
| Broker weg | Retry 1 s bis 10 s, Meldung je Versuch, Stände gezählt und verworfen, danach aktueller Stand |
| Ungültiges Register | Fehler sichtbar, `Bad` |

## Akzeptanzkriterien
| ID | Kriterium | Ergebnis | Beleg |
|---|---|---|---|
| AC-1 | Broker startet aus Compose, `docker compose ps` healthy | offen | |
| AC-2 | Alle 4 Gerätetypen kommen im Viewer an | offen | |
| AC-3 | Sollwert ändern: sichtbar im Simulator, im Modbus-Read, in der Nachricht | offen | |
| AC-4 | Pause: `simTimeUtc` stimmt, keine neue Nachricht, Resume läuft weiter | offen | |
| AC-5 | Gerät beenden: `Bad` + Fehlermeldung, Neustart: wieder `Good` | offen | |
| AC-6 | Broker stop/start: Reconnect-Meldungen, Zähler, aktueller Stand danach | offen | |
| AC-7 | Alle Apps und Broker beenden kontrolliert | offen | |
| AC-8 | Kein Secret im Repo, `.env.example` vorhanden | offen | |
| AC-9 | Diagnostic-Client und `testrun_task2.sh` laufen weiter | offen | |

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (SRS LB03)
