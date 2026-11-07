# EnergySim.Cli

Gemeinsamer ausführbarer Startpunkt für PV, Batterie, Ladestation und Wind.
Die Anwendung erstellt pro Start genau einen Simulator mit lokaler Uhr, gemeinsamer
Anzeige und optionalem CSV-Verlauf. Wind-Anbindung: Jonas Kreibich. Die weiteren
Modulverantwortlichkeiten werden im Team festgelegt.

## Voraussetzungen und Start

.NET 10 gemäß `global.json`; Pakete einmal wiederherstellen. Aus dem
Repository-Hauptverzeichnis:

```powershell
dotnet build EnergySim.sln
dotnet run --project src/EnergySim.Cli -- --help
dotnet run --project src/EnergySim.Cli -- --device wind
dotnet run --project src/EnergySim.Cli -- --device wind --location salzburg --start 2026-10-23T13:00:00Z --csv wind-run.csv
```

Ohne `--device` öffnet eine interaktive Konsole die gemeinsame Geräteauswahl.
Bei umgeleiteter Eingabe ist `--device` erforderlich. Visual Studio: Dieses Projekt
als Startprojekt und eines der Geräteprofile wählen, für Wind `Wind` oder
`Wind (3600x)`. `Pick from menu` öffnet die Auswahl.

## Gemeinsame Konfiguration

| Argument | Standard / Bedeutung |
|---|---|
| `--device` | `pv`, `battery`, `charger`, `wind`; interaktiv Auswahl bei fehlendem Wert |
| `--id` | Gerätewahl plus `-1`, beispielsweise `wind-1` |
| `--location` | `salzburg`; alternativ `wien`, `innsbruck` |
| `--start` | `2026-06-21T04:00:00Z`; virtuelle Startzeit, intern UTC |
| `--speed` | 60; erlaubter Bereich 0,1 bis 3600 |
| `--weather-url` | `http://localhost:5200`; Wetter-Prozess für PV und Wind, Pflicht |
| `--csv` | Kein CSV ohne Pfad; eine vorhandene Zieldatei wird ersetzt |

Die Anwendung liest `appsettings.json` aus ihrem Ausgabeordner, beim Build aus
diesem Projekt kopiert. Kommandozeilenwerte überschreiben JSON-Werte. Die
Geräteabschnitte heißen `Pv`, `Battery`, `Charger`, `Wind`; Klassenstandardwerte
gelten bei fehlenden Einstellungen. Beispiel: `--Wind:RatedPowerKw 250`.
Die vollständigen Windparameter stehen im README von `EnergySim.Wind`.

## Bedienung und Ablauf

Gemeinsame Gerätetasten: O ein, F aus, L Leistungslimit, T Störung, R Fehlerreset.
P pausiert/setzt fort, V setzt das Tempo, + und - ändern es, Q beendet den Lauf.
Weitere Gerätebefehle stammen aus dem jeweiligen Adapter. Bei umgeleiteter
Ein-/Ausgabe beendet `quit` oder das Ende der Eingabe den Lauf; die Ausgabe ist
dann ein Statusprotokoll statt des interaktiven Dashboards.

`DeviceFactory` bindet Optionen und erstellt das Gerät. `CliRunner` verbindet es
mit `LocalClock`, `DeviceSimulationLoop`, Terminal und optionalem `CsvHistoryWriter`.
Wetterabhängige Geräte (PV, Wind) erhalten einen `HttpWeatherClient`. Der Wetter-Prozess
(`EnergySim.WeatherService`) muss laufen, sonst Abbruch beim Start mit Hinweis. `ClockDashboardSource` leitet Uhrbefehle an die lokale Uhr
und Gerätebefehle über den gemeinsamen Befehlskanal weiter.

## Tests und Einschränkungen

`dotnet test EnergySim.sln` prüft unter anderem Einstellungen, Factory, Hilfe und
die gemeinsamen Gerätebefehle. Die Hilfeprüfung läuft isoliert, weil sie die
Konsolenausgabe vorübergehend in den Speicher umleitet. Ein Wind-Laufversuch ist
unter `docs/evidence/wind-local-run.log` im Repository dokumentiert.

Mit `--clock local` (Standard) läuft eine eigene Uhr, nicht mit anderen Prozessen
synchron. Mit `--clock shared` übernimmt das Gerät die Zeit vom Master
(`EnergySim.Time.Cli`, Port 1502). Jedes Gerät hat einen eigenen Modbus-Server
(`--Modbus:Port`, Standard 1511 pv, 1512 battery, 1513 charger, 1514 wind) und einen
Clock-Endpunkt (`--clock-port`, 1601 bis 1604). Registerpläne: README des Geräts.
Mehrere lokale Instanzen brauchen verschiedene IDs und CSV-Ziele. Windstille
beim Standardstart kann korrekt zu `Standby` und 0 W führen. Fünf bekannte
CSV-Dateisperrtests unter Windows scheitern bereits im Ausgangsstand; sie wurden
durch die Wind-Anbindung nicht verändert.

## Shared clock

Runs the PV, battery, or EV charger simulator. The default clock is local to that process. To use the shared time service, start the service separately and pass `--clock shared`, for example `dotnet run --project src/EnergySim.Cli -- --device pv --clock shared`.

Shared-clock options are `--time-host` (loopback only), `--time-port`, and `--clock-port`. A shared-mode device hides local clock controls because the time service owns pause, resume, speed, and restart operations.

If the shared time service disappears, a previously initialized device keeps using the last received time, speed, and run/pause state. A connection warning appears after heartbeat expiry (3 seconds by default), and registration is retried every second by default. Once connected again, the device adopts the service's clock settings. Reconnecting to the same session and generation preserves model state; a new service session or generation resets the model to that timeline. A newly launched service starts stopped, so press `P` on its dashboard to start the connected devices again. Devices that have never received an initial clock definition keep waiting for initialization.

## Device CSV export

Devices that provide `CsvHistoryFields` write one CSV row for each new simulation state, including accepted state changes while paused. Virtual-time step size therefore controls the spacing between data points. The CSV is a background sink and does not add a dashboard panel.

By default, exports are written to `data/export/<device>_YYYY_MM_DD.csv` under the current working directory, using the local date when the simulator starts. The charger currently exports the legacy columns in their original order. Use `--csv <path>` to choose another location. Devices without `CsvHistoryFields` retain the generic interval exporter and only write CSV when `--csv` is supplied.

To add device-specific CSV columns, populate `CsvHistoryFields` from the device's model state in its own project. Keep the field names and order stable. The CLI detects a non-empty field list, creates the default dated path, and writes each new frame automatically, so a device implementation does not need changes to the CLI or shared CSV writer.
