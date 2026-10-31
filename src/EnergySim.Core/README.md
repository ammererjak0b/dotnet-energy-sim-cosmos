# EnergySim.Core

Gemeinsame Verträge und Infrastruktur der Simulatoren. Teammodul; der zusätzliche
Gerätetyp Wind in dieser Lieferung stammt von Jonas Kreibich. Die eindeutige
Gesamtverantwortung für Core ist im Team festzulegen.

## Inhalt und Schnittstellen

- `Devices`: `ISimulatedDevice`, `SimulatedDeviceBase`, gemeinsamer `DeviceHeader`,
  Befehle, Anzeigezeilen und Zustandswerte.
- `Engine`: Uhrvertrag `IClockSource`, lokale Uhr, Geräteschleife,
  `SimulationFrame` und Ausgabeziele über `ISimulationSink`.
- `Weather`: Datenvertrag `WeatherDto` und asynchroner Wetterclientvertrag.
- `History`: Gemeinsamer asynchroner CSV-Verlauf aus Simulationsframes.

Geräte implementieren `update`, `reset` und die gemeinsamen Befehle. Die
Basisklasse verarbeitet O/F/L/T/R und prüft den Bereich des Leistungslimits.
Zeit und Wetter werden dem Gerät übergeben; das Gerätemodell lädt sie nicht selbst.

`DeviceType` verwendet die stabilen Werte `Pv = 1`, `Battery = 2`, `Charger = 3`
und `Wind = 4`. Der Kopf enthält Wirkleistung in Watt und kumulierte Energie in
Wattstunden. Erzeugung ist positiv, Verbrauch negativ. Die fachliche Bedeutung
des Energiezählers und zusätzlicher Werte wird im jeweiligen Gerät dokumentiert.
Virtuelle Zeit und Sequenz liegen im `SimulationFrame` außerhalb des Gerätekopfs.

## Konfiguration und Verwendung

Core ist eine Bibliothek und kein startbares Programm. Die CLI erstellt die
Komponenten und übergibt unter anderem Standort, Tickdauer, maximale virtuelle
Schrittlänge und Ausgabeziele. Core hat keine eigene Konfigurationsdatei und
keine Referenz auf ein Gerätemodul. Es verwendet die gemeinsamen .NET-10-Einstellungen.

## Bauen und testen

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet build EnergySim.sln
dotnet test EnergySim.sln
```

Das gemeinsame Testprojekt prüft Uhr, Loop, Befehle und CSV. Geräteadaptertests
prüfen zusätzlich die Verwendung des Vertrags und die Einheiten des Kopfes.

## Bekannte Einschränkungen

`LocalClock` ist eine Uhr für einen Prozess; die zentrale Synchronisation folgt
in einem weiteren Teammodul. Die Wetterqualität `IsStale` ist im Ergebnis des
Clients und im Frame sichtbar, wird aber nicht als eigenes Argument an Geräte
weitergereicht. Die fünf bekannten Dateisperrfehler in `CsvHistoryWriterTests`
unter Windows sind im Ausgangsstand nachgewiesen und in diesem Schritt nicht
behoben. Core implementiert noch keinen gemeinsamen Modbus-Registermapper.
