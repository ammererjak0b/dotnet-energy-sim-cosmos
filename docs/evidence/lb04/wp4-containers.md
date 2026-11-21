# WP4 containers: raw checks

State: branch `lb04/wp4-containers`, uncommitted working tree on top of `4def27b` (final commit hash is added in WP8). Host: Linux, Docker 29.9.3, Compose 5.7.1. Config: `compose.yaml` as in the repo. Manual function checks, not automated tests.

## Build and start
- `docker compose build`: images `energysim/master:lb04` 326M, `energysim/weather:lb04` 326M, `energysim/device:lb04` 329M
- `docker compose up -d --wait rabbitmq master weather wind-1`: all healthy

## Master log (id as host name)
```
Registered wind-1 on wind-1:1604.
wind-1 is OK.
clock: Running speed 60 revision 2 generation 1 virtual 2026-06-21 06:10 CEST (04:10Z)
  wind-1 @ 1604 | OK | last seen 13:29:19Z | missed 0 | skew 0 ms
```

## Weather query by the wind container (weather log)
```
salzburg 2026-06-21 04:12 => T=13.1C cloud=0.0% G=40.2W/m2 sun=8.1deg wind=1.3m/s rain=0.0mm/h (5 requests)
```
`curl 'http://127.0.0.1:5200/api/weather?location=salzburg&time=2025-07-01T12:00:00Z'` answers 200 JSON (`WindSpeedMs` 1.83).

## Diagnostic client against the published port 1514
```
type Wind, state Running, power 4841 W, energy 7057 Wh, seq 261, clock Running
device: WindSpeed 4.8 m/s, RotorSpeed 6 rpm, NacelleDirection 300.8 deg
> write limit 50 -> effect: state Running, power 4542 W (was 4841 W)
```

## Data flow container -> Modbus -> native gateway -> RabbitMQ -> native viewer
```
[wind-1] sim 2026-10-23 13:41:35Z  seq 287  Good  Running
    ActivePower = 3436 W   Energy = 7398 Wh   WindSpeed = 4.4 m/s
```
Gateway: `wind-1 seq 302 Good published`.

## Master commands via `docker attach master`
```
pause  -> clock: Paused ... revision 7
step   -> Step scheduled, revision 8
step   -> Step scheduled, revision 9
resume -> clock: Running ... revision 10
```
Virtual time shows minutes only, 2 steps = 24 s virtual are not visible there. The device received every revision (participant OK, missed 0).

## Controlled stop
`docker compose stop` (after the SIGTERM fix, see `decisions.md`): wind-1, master, weather exit code 0, 0.6 s. Before the fix wind-1 ended with 137 after the 10 s grace time (Ctrl+C/SIGTERM did not end a headless device with open stdin).

## Failed or open
- First attempt `docker attach` through a pipe in one command line hung, solved with `timeout -s KILL`, documented in `docker.md`
- Wind was `Standby` with 0 W at the first start time (wind 1.3 m/s below cut-in), start time changed to 2026-10-23 13:00Z
- Not checked: Windows/macOS Docker Desktop, second wind container, native devices next to a containerized master
