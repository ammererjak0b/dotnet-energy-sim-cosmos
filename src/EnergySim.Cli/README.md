# EnergySim.Cli

Runs the PV, battery, or EV charger simulator. The default clock is local to that process. To use the shared time service, start the service separately and pass `--clock shared`, for example `dotnet run --project src/EnergySim.Cli -- --device pv --clock shared`.

Shared-clock options are `--time-host` (loopback only), `--time-port`, and `--clock-port`. A shared-mode device hides local clock controls because the time service owns pause, resume, speed, and restart operations.
