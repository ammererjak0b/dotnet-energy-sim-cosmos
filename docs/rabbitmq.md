# RabbitMQ unter Docker

- Image: `rabbitmq:4.3.7-management`
- Ports: `5672` AMQP, `15672` Management-UI (`http://localhost:15672`)
- Plugins: keine zusätzlichen (kein MQTT)

## Zugangsdaten
```bash
cp .env.example .env   # .env ist in .gitignore
# RABBITMQ_USER und RABBITMQ_PASSWORD in .env setzen
```
Apps lesen `RABBITMQ_USER` und `RABBITMQ_PASSWORD` aus der Umgebung. In der Shell laden: `set -a; source .env; set +a`.

## Betrieb
```bash
docker compose up -d     # Start
docker compose ps        # Status, STATUS = healthy
docker compose stop      # Stopp, Daten bleiben
docker compose down      # Container weg, Volume bleibt (-v löscht es)
```
Check: UI-Login mit den Werten aus `.env`, Exchange `energysim.telemetry` erscheint sobald Gateway oder Viewer laufen.

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (RabbitMQ Docker)
