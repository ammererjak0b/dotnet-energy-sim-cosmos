# Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Device aborts at start with weather hint | weather process not running | start `WeatherService` first |
| Device shows warning, master not found | master not running or wrong `--time-port` | start master, device retries every 1 s |
| Devices show `Running` but time stands still | master clock stopped | press `P` in the master |
| After master restart devices stop | new master starts stopped | press `P` again |
| `step` or `S` rejected | clock not paused, or a change is still pending | pause first, wait a moment between steps (lead time 100 ms by default, `--lead-ms`) |
| Master shows `UNREACHABLE` | device process gone | restart the device, it registers again |
| Port in use, exit code 1 | other process on that port | stop it or choose another port. Ports below 1502 are rejected |
| Second device of a type fails | duplicate id or port | set own `--id`, `--clock-port`, `--Modbus:Port` |
| Another master answers on 1502 | old master still running | stop it, or set `--time-port` on all devices |
| PV or wind 0 W, `Standby` | no wind or sun at that time and place (correct) | other `--start` or `--location` |
| Weather warning `stale` | weather process down | restart it, last value is used meanwhile |
| Diagnostic: connection refused | device not running or wrong port | start device, check `--Port` |
| Diagnostic: write rejected | limit outside 0..100 | use a valid value |
| Warn flag `InvalidWrite` | write outside range, value restored | next valid write clears it |
| Compose: `set in .env` error | `.env` missing | `cp .env.example .env`, set the password |
| Gateway or viewer: broker retries | RabbitMQ down | `docker compose up -d --wait`, they reconnect (1 s to 10 s) |
| Viewer shows `Bad` for a device | device stopped | start it, next message is `Good` |
| Gateway counts dropped states | broker was down | expected, nothing is buffered, current state comes after reconnect |
| Fault does not reset (battery) | cell temperature above reset limit | wait until below 40 degC |

Docker problems: see [[user-documentation/docker]].

## Limits
- Default: all on one machine (clock endpoints loopback). Docker: one host with a bridge network, see [[user-documentation/docker]]
- A master started with `--dial-device-id` (container) reaches devices only by their id as host name, native devices on the host cannot join it
- Weather: reference year 2025, 3 locations
- Device Modbus servers have no authentication and bind `0.0.0.0` by default, use `--Modbus:Host 127.0.0.1` outside a trusted network
- Reads can be torn (state changes mid-read), compare `sequence` before and after
