# EnergySim.Messaging

Telemetry-Nachricht, Routing und RabbitMQ-Anbindung (AMQP 0-9-1, `RabbitMQ.Client` 7.x). Einziges Projekt mit RabbitMQ.Client. Vertrag: [docs/contracts/messaging.md](../../docs/contracts/messaging.md).

| Teil | Inhalt |
|---|---|
| `TelemetryMessage`, `Measurement`, `TelemetryQuality` | Nachricht |
| `TelemetryJson` | JSON, camelCase, Enums als Text |
| `Routing` | Exchange `energysim.telemetry`, Routing Key `telemetry.<type>.<id>` |
| `RabbitOptions` | Host, Port, VirtualHost, Exchange aus Config, User/Passwort aus `RABBITMQ_USER`/`RABBITMQ_PASSWORD` |
| `TelemetryPublisher` | publish, kein Auto-Recovery, `IsOpen` |
| `TelemetryConsumer` | eigene exclusive Queue, `Closed` meldet Verbindungsverlust |
| `BrokerUnavailableException` | Broker weg, versteckt RabbitMQ-Exceptions |

Referenzen: keine eigenen Projekte.
