# Present the LB04 result

Short guide: get everything running and show it. About 15 minutes for the whole demo. Back to [[user-documentation/index|User documentation]].

## The day before (once, 15 minutes)
1. `git switch` to the state you present, `git status` is clean
2. `cp .env.example .env` and set `RABBITMQ_PASSWORD` (not in git)
3. `dotnet build EnergySim.sln` (0 warnings) and `dotnet test EnergySim.sln` (all passed)
4. `docker compose build` (first time downloads the images)
5. Dry run of both parts below, then `scripts/run-simulation.sh stop`
6. Portainer: start it once, open `https://localhost:9443`, create the admin user (only possible in the first 5 minutes after the start)

Windows: use `powershell -ExecutionPolicy Bypass -File .\scripts\run-simulation.ps1 ...` instead of `scripts/run-simulation.sh ...`. Docker Desktop must run.

## Part A: all four devices, native (7 minutes)
Shows: time, weather, 4 simulators, Modbus, RabbitMQ, viewer.
1. `scripts/run-simulation.sh` (opens windows: master, weather, battery-1, charger-1, pv-1, wind-1, diagnostic, viewer, gateway)
2. Master window: press `P`. The clock starts, all devices follow
3. Viewer window: one block per device state (`pv-1`, `battery-1`, `charger-1`, `wind-1`): sim time, power, energy
4. Change something: diagnostic window `write limit 40`. The PV power drops, the viewer shows the new value a moment later
5. Pause: master window `P`. The viewer stops getting new states, the sim time stands still. `S` = one single step (+12 s virtual at speed 60). `P` again = resume
6. Speed: `V`, type `600`, Enter. Time runs 10x faster, `V` `60` back
7. Optional: stop one device window with `Q`. The gateway window and the viewer show `BAD` for it once. Start it again with its command from the README (start order, step 3), then it is `Good` again

## Part B: containers (5 minutes)
Shows: master, weather, wind-1 as Docker containers, Portainer, same data path.
1. `scripts/run-simulation.sh stop`, then `scripts/run-simulation.sh docker`
2. `docker compose ps`: master, weather, wind-1, rabbitmq, portainer are up (healthy)
3. Portainer `https://localhost:9443` (accept the certificate warning): the containers, logs of `master` ("Registered wind-1 on wind-1:1604")
4. Viewer window shows `wind-1` values, diagnostic window `write limit 50` changes the power
5. Pause and step through the container master: `docker attach master`, type `pause`, `step`, `step`, `resume`, leave with Ctrl+P then Ctrl+Q (not Ctrl+C)
6. Weather proof: `curl 'http://127.0.0.1:5200/api/weather?location=salzburg&time=2025-07-01T12:00:00Z'`. The wind container takes its wind speed from it (log of `weather`)
7. Controlled stop: `docker compose stop`, then `docker inspect -f '{{.Name}} {{.State.ExitCode}}' master weather wind-1`: all 0

The gateway window reports `pv-1`, `battery-1`, `charger-1` as not reachable once. They do not run in this mode, that is correct.

## Show the tests (2 minutes)
- `dotnet test EnergySim.sln`: unit tests and integration tests, all passed
- `dotnet test tests/EnergySim.IntegrationTests`: real Modbus server + real client
- Optional: `scripts/proof-run.sh proof-logs` runs the whole check without windows (about 2 minutes) and writes `proof-logs/steps.log` with exit codes 0

## Where the proof and the documents are
| What | Where |
|---|---|
| Requirements and status | `docs/requirements.md` |
| Work list, open items | `docs/todo.md` |
| What was found and changed | `docs/technical-documentation/consolidation-lb04.md` |
| Decisions and reasons | `docs/technical-documentation/decisions.md` |
| Test selection | `docs/technical-documentation/test-and-evidence.md` |
| Run logs and checks | `docs/evidence/lb04/` |
| Docker guide | `docs/user-documentation/docker.md` |
| Every project explained | `src/<project>/README.md` (top table) |

## If something goes wrong
| Problem | Fix |
|---|---|
| "ports busy" | `scripts/run-simulation.sh stop`, run again |
| Nothing in the viewer | gateway window running? broker up (`docker compose ps`)? clock started (native: `P` in the master)? |
| Wind shows 0 W | no wind at that virtual time, that is correct. Docker mode starts at a windy time, native at 2026-06-21 04:00 |
| `.env` missing | `cp .env.example .env`, set the password |
| Windows: script blocked | start with `powershell -ExecutionPolicy Bypass -File ...` |
| Container shows old behavior | `docker compose build`, then start again |
| Everything is stuck | `scripts/run-simulation.sh stop`, `docker compose down`, start again |

More: [[user-documentation/docker]], [[user-documentation/troubleshooting]].
