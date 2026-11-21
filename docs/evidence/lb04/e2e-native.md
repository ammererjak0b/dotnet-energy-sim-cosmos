# Shared end-to-end run (native, all four devices)

Manual function check, driven by a one-off script (headless, every program with its own stdin), logs in [e2e-native/](e2e-native/). Date 2026-11-21, Linux. State checked: branch `lb04/wp8-evidence` (tip `b161cc1` plus the viewer/gateway fix of this run, see the final commit).

Config used:
- RabbitMQ: container from `compose.yaml` (`rabbitmq:4.3.7-management`), credentials from `.env`
- Master `--headless --start 2026-06-21T10:00:00Z --speed 60`, weather service, 4 devices `--clock shared` (pv-1, battery-1, charger-1, wind-1), diagnostic client on pv-1 (port 1511), viewer, gateway (defaults of `appsettings.json`)
- Master, devices, weather: `dotnet <dll>` in `src/*/bin/Debug/net10.0`

## Result per acceptance criterion
| AC | Check | Result | Where |
|---|---|---|---|
| AC-1 | `docker compose ps`: `energysim-rabbitmq Up (healthy)` | pass | steps.log line 1 |
| AC-2 | all four types arrive: 8 messages each in the first 8 s (pv-1, battery-1, charger-1, wind-1) | pass | steps.log, viewer.log |
| AC-3 | `write limit 40` on pv-1: Modbus header before `Running` 3132 W, after `Limited` 2000 W. Simulator window `State Limited, Active power 2.00 kW`. Viewer `[pv-1] ... Good Limited, ActivePower = 2000 W` | pass | steps.log, diag-pv-1.log, pv-1.log, viewer.log |
| AC-4 | pause: 68 messages right after the pause, 68 six seconds later (0 new). Sim time stands at `10:15:01Z`. Two `step`: `10:15:13Z`, `10:15:25Z` (+12 s each, one message each). Resume: `10:20:17Z`, runs on | pass | steps.log |
| AC-5 | `quit` at wind-1 (exit 0): exactly 1 `Bad` message with the reason (`lost connection to 127.0.0.1:1514 ...`), no sim time. Restart of wind-1: `[wind-1] ... Good Standby` again | pass | steps.log, viewer.log |
| AC-6 | broker stopped 20 s: gateway and viewer log `broker: cannot connect ..., next try in 1000/2000/4000 ms`, gateway counts `dropped ... (broker down)` and reports `broker connected, dropped so far: 76`. After the start both reconnected, newest message is the current state (`pv-1 seq 343`) | pass | gateway.log, viewer.log |
| AC-7 | `quit` to devices, diagnostic, master, Ctrl+C-style stop (SIGINT) to gateway, viewer, weather: exit codes 0 for all nine, `docker compose down` | pass | steps.log, last lines |
| AC-8 | `git ls-files .env` = nothing, `.env.example` tracked with placeholders (`change-me`), no password in code (the C# code only reads the variables) | pass (code reading) | repo |
| AC-9 | diagnostic client works (header, write), run scripts: `proof-run.sh`, `proof-run.ps1` (in PowerShell 7), `run-simulation.sh docker/pv2/stop`, see [[evidence/lb04/scripts]] | pass on Linux, Windows open | scripts.md |

## Mapping of the values to virtual time
- The gateway reads the device header from Modbus. `simTimeUtc` in the message is the **virtual time** of the device state (header registers 9 to 12). `polledAtUtc` is the **real time** of the poll. They are never mixed
- At speed 60 one real second is one virtual minute. Started at 10:00:00Z virtual with `start` at real 15:44:39: about 15 real seconds later the viewer shows `sim 10:13:57Z`, matching 14 s x 60 = 14 min
- While paused the virtual time and the sequence number stand still, the gateway publishes only a change of state (the pause itself) and then nothing. A `step` adds `200 ms x speed` = 12 s virtual and one new state, so one message
- After a resume the time continues from the paused value (10:15:25Z -> 10:20:17Z after 6 real seconds with the 2 step seconds, no jump)

## Found during this run (fixed)
First attempt: viewer and gateway printed nothing and published nothing. Cause: their quit-watcher read `Console.In` on the starting thread, which blocks when stdin is a pipe (same bug class as the `HeadlessRunner` one from WP4). Fix: `Task.Run` in `ViewerRunner` and `GatewayRunner`. Second and third run (this evidence): pass. Not unit tested (private method), only by this run.
Also: the first run used a start time in October (sun low): the limit of 40 % did not bind. The run in this folder starts at 2026-06-21 10:00Z (PV 3132 W unlimited).

## Limits
- One machine, loopback. Pv, battery, charger and wind are native processes here, the container run is in [[evidence/lb04/wp4-containers]]
- The run is scripted (not typed by hand in windows). Same programs, same commands as in the presentation guide
- Windows not covered
