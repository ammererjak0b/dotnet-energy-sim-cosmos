# EnergySim.Cli

Runs the PV, battery, or EV charger simulator. The default clock is local to that process. To use the shared time service, start the service separately and pass `--clock shared`, for example `dotnet run --project src/EnergySim.Cli -- --device pv --clock shared`.

Shared-clock options are `--time-host` (loopback only), `--time-port`, and `--clock-port`. A shared-mode device hides local clock controls because the time service owns pause, resume, speed, and restart operations.

If the shared time service disappears, a previously initialized device keeps using the last received time, speed, and run/pause state. A connection warning appears after heartbeat expiry (3 seconds by default), and registration is retried every second by default. Once connected again, the device adopts the service's clock settings. Reconnecting to the same session and generation preserves model state; a new service session or generation resets the model to that timeline. A newly launched service starts stopped, so press `P` on its dashboard to start the connected devices again. Devices that have never received an initial clock definition keep waiting for initialization.

## Device CSV export

Devices that provide `CsvHistoryFields` write one CSV row for each new simulation state, including accepted state changes while paused. Virtual-time step size therefore controls the spacing between data points. The CSV is a background sink and does not add a dashboard panel.

By default, exports are written to `data/export/<device>_YYYY_MM_DD.csv` under the current working directory, using the local date when the simulator starts. The charger currently exports the legacy columns in their original order. Use `--csv <path>` to choose another location. Devices without `CsvHistoryFields` retain the generic interval exporter and only write CSV when `--csv` is supplied.

To add device-specific CSV columns, populate `CsvHistoryFields` from the device's model state in its own project. Keep the field names and order stable. The CLI detects a non-empty field list, creates the default dated path, and writes each new frame automatically, so a device implementation does not need changes to the CLI or shared CSV writer.
