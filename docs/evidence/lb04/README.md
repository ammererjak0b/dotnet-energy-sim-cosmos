# LB04 evidence

Index. State checked: see the tag `lb04-2026` (full commit hash is in [[todo]] and in the journal entry of the submission). Evidence is separated: **automated tests** / **manual function checks** / **code reading only**. Failed checks and limits are in each file.

| Required evidence | What | Kind | File |
|---|---|---|---|
| 1 Consolidation | problems found, fixes, checks, links to SRS, docs, to-do | code reading + tests | [[technical-documentation/consolidation-lb04]], [[evidence/lb04/wp6-consolidation]], [[requirements]], [[todo]] |
| 2 Automated tests | full run with counts, the integration tests, selection and prerequisites | automated | [[evidence/lb04/tests]], [[technical-documentation/test-and-evidence]], `tests/README.md` |
| 3 Container per person | build and start from versioned config: master (Leon), weather (Jakob), wind-1 (Jonas): ids, connection settings, joined the system | manual | [[evidence/lb04/wp4-containers]], `compose.yaml`, [[user-documentation/docker]] |
| 4 Data flow | containerized wind: Modbus -> gateway -> RabbitMQ -> viewer. Containerized weather: query + use in the device | manual | [[evidence/lb04/wp4-containers]] |
| 5 Shared run | setpoint -> viewer, pause, step, resume, mapping to virtual time | manual | [[evidence/lb04/e2e-native]] |
| 6 Controlled shutdown | all programs exit 0, containers stop in about 1 s | manual | [[evidence/lb04/e2e-native]], [[evidence/lb04/wp4-containers]], [[evidence/lb04/scripts]] |
| Scripts | Linux and Windows run scripts | manual | [[evidence/lb04/scripts]] |
| Single step, integration test | time control and real interface | automated + manual | [[evidence/lb04/wp5-integration-test]] |
| Older stages | LB02 proof run | manual | [[evidence/README]] |

## Config used
`compose.yaml` (RabbitMQ 4.3.7-management, master, weather, wind-1, Portainer 2.21.5), `.env` from `.env.example` (placeholders only), master `--start` and `--speed` as written in each file.

## Failed checks and limits (all of LB04)
- Per-person proof: the three containers were started together from the versioned config on one machine in the team call. Each person should start their own container once on their own machine (open in [[todo]])
- Windows scripts not run on Windows, IDE test run not done, Windows/macOS Docker Desktop not checked
- Not unit tested: the `Task.Run` fix in viewer and gateway (verified by the shared run)
- First attempts that failed and were fixed: wind-1 exit code 137 on `docker stop` (WP4), viewer/gateway silent with piped stdin (WP8)
