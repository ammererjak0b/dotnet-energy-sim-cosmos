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
Wetterabhängige Geräte erhalten einen `LocalWeatherClient` mit den lokalen
Referenzjahrdateien. `ClockDashboardSource` leitet Uhrbefehle an die lokale Uhr
und Gerätebefehle über den gemeinsamen Befehlskanal weiter.

## Tests und Einschränkungen

`dotnet test EnergySim.sln` prüft unter anderem Einstellungen, Factory, Hilfe und
die gemeinsamen Gerätebefehle. Die Hilfeprüfung läuft isoliert, weil sie die
Konsolenausgabe vorübergehend in den Speicher umleitet. Ein Wind-Laufversuch ist
unter `docs/evidence/wind-local-run.log` im Repository dokumentiert.

Die lokale Uhr wird noch nicht zwischen Prozessen synchronisiert. Es gibt hier
noch keinen Simulations-Master, eigenständigen Wetterserver oder Modbus-Server.
Mehrere lokale Instanzen brauchen verschiedene IDs und CSV-Ziele. Windstille
beim Standardstart kann korrekt zu `Standby` und 0 W führen. Fünf bekannte
CSV-Dateisperrtests unter Windows scheitern bereits im Ausgangsstand; sie wurden
durch die Wind-Anbindung nicht verändert.
