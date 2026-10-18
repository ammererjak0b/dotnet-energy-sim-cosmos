# Batteriespeicher-Simulator – Aufgabe 01

Konsolenanwendung zur Simulation eines Batteriespeichers über 24 Stunden. Das
Programm unterstützt einen automatischen und einen manuellen Betrieb, beachtet
Leistungs- und SOC-Grenzen, simuliert eine Übertemperaturstörung und schreibt
den Verlauf in eine CSV-Datei.

## Voraussetzungen

- Visual Studio 2026 mit .NET-Desktopentwicklung oder .NET SDK 10
- Das in `global.json` festgelegte .NET-10-SDK

Ein zusätzliches NuGet-Paket ist nicht notwendig. Für Aufgabe 01 wurde bewusst
noch kein Unit-Test-Projekt angelegt. Spätere Unit-Tests werden mit xUnit
erstellt.

## Starten

### In Visual Studio

1. `dotnet-energy-sim-lb01.sln` öffnen.
2. `BatteryStorageSimulator` als Startprojekt auswählen.
3. Mit `Strg+F5` starten.

### Im Terminal

```powershell
dotnet run --project src/BatteryStorageSimulator
```

Optionale Argumente:

```powershell
dotnet run --project src/BatteryStorageSimulator -- --help
dotnet run --project src/BatteryStorageSimulator -- --speed 1000000
dotnet run --project src/BatteryStorageSimulator -- --config C:\Pfad\eigene-settings.json
```

Der Standardwert `speedFactor: 600` bedeutet: Zehn simulierte Minuten
vergehen in einer realen Sekunde. Ein simulierter Tag dauert damit ungefähr
2 Minuten und 24 Sekunden.

## Bedienung

Während die Simulation läuft, öffnet die Taste `M` das Pausenmenü. Dort kann
man:

- zwischen Auto-Modus und manuellem Betrieb wechseln,
- Lade- oder Entladeleistung einstellen,
- die manuelle Leistung auf 0 kW setzen,
- Auto-Leistungslimit, Geschwindigkeit und Schrittweite ändern,
- eine Übertemperaturstörung auslösen und zurücksetzen,
- die Simulation fortsetzen oder beenden.

Vorzeichen der Leistung:

- positive Leistung: Batterie entlädt,
- negative Leistung: Batterie lädt,
- 0 kW: Bereitschaft oder Begrenzung durch SOC/Störung.

## Standardkonfiguration

Die Werte stehen in
`src/BatteryStorageSimulator/batterysettings.json` und können ohne Änderung am
C#-Code angepasst werden.

| Wert | Standard |
|---|---:|
| Geräte-ID | `BAT-01` |
| Technologie | LFP |
| Nennkapazität | 100 kWh |
| maximale Lade-/Entladeleistung | 50 kW |
| Start-SOC | 50 % |
| erlaubter SOC | 10 bis 90 % |
| Lade-/Entladewirkungsgrad | jeweils 95 % |
| Simulationsdauer | 24 Stunden |
| Schrittweite | 15 Minuten |
| Geschwindigkeit | 600x |

Ungültige Einstellungen werden beim Programmstart mit einer verständlichen
Fehlermeldung abgelehnt.

## Simulationsmodell

Für jeden Zeitschritt gilt grundsätzlich `Energie = Leistung × Zeit`.

- Laden: gespeicherte Energie steigt um
  `Ladeleistung × Zeit × Ladewirkungsgrad`.
- Entladen: gespeicherte Energie sinkt um
  `Entladeleistung × Zeit / Entladewirkungsgrad`.
- SOC: `gespeicherte Energie / Nennkapazität × 100`.

Der Speicher überschreitet weder die konfigurierte Leistung noch die SOC-Grenzen.
Der Auto-Modus verwendet mit einem festen Zufalls-Seed einen reproduzierbaren
Tagesverlauf: nachts und mittags wird überwiegend geladen, morgens und abends
überwiegend entladen. Das ist ein vereinfachtes Demonstrationsmodell und keine
Prognose eines realen Gebäudes oder Strommarkts.

Die Temperatur nähert sich abhängig von der aktuellen Leistung einem Zielwert.
Ab 40 °C erscheint eine Warnung. Bei 50 °C entsteht eine
Übertemperaturstörung; dann ist die Ist-Leistung 0 kW. Ein Reset ist erst bei
höchstens 40 °C möglich.

## Anzeige und CSV

Die Konsole zeigt unter anderem Simulationszeit, Modus, Soll-/Ist-Leistung,
gespeicherte Energie, SOC mit Balken, Temperatur, kumulierte Lade- und
Entladeenergie sowie den Fehlerzustand.

Während des normalen Laufs werden die bestehenden Konsolenzeilen direkt
überschrieben. Dadurch muss das Fenster nicht bei jedem Simulationsschritt
geleert werden und die Anzeige flackert deutlich weniger. Beim Öffnen oder
Schließen des Menüs wird die Ansicht einmal neu aufgebaut.

Nach jedem Schritt und bei Bedienereignissen wird
`data/battery-run.csv` neu beschrieben. Die Datei verwendet standardmäßig
Semikolon und österreichische Dezimalzahlen. Sie lässt sich zum Beispiel in
Excel öffnen und für Kurven verwenden.

Ist `battery-run.csv` gerade in einem anderen Programm geöffnet und deshalb
für das Schreiben gesperrt, legt der Simulator automatisch eine zusätzliche
Datei wie `battery-run-20261004-184500.csv` an. Der tatsächlich verwendete Pfad
wird am Ende in der Konsole angezeigt.

## Projektstruktur

```text
dotnet-energy-sim-lb01/
|-- src/BatteryStorageSimulator/
|   |-- Configuration/   JSON-Einstellungen und Prüfung
|   |-- Domain/          Batteriemodell und Zustände
|   |-- Persistence/     CSV-Ausgabe
|   |-- Presentation/    Konsolenanzeige und Eingaben
|   |-- Simulation/      Zeitablauf, Auto-Modus und Menü
|   |-- batterysettings.json
|   `-- Program.cs       Programmeinstieg
|-- data/                erzeugter CSV-Beispiellauf
|-- docs/
|   |-- development-journal/
|   `-- screenshots/
`-- dotnet-energy-sim-lb01.sln
```

## Modellbezug und Quellen

Die gewählten Werte sind plausible Lehrbeispielwerte und keine Auslegung für
eine konkrete Anlage. Zum Vergleich und zur eigenen Prüfung:

- [HTW Berlin – Stromspeicher-Inspektion 2026](https://solar.htw-berlin.de/wp-content/uploads/HTW-aquu-Stromspeicher-Inspektion-2026.pdf)
- [NREL – Utility-Scale Battery Storage, Annual Technology Baseline](https://atb.nrel.gov/electricity/2024/2023/utility-scale_battery_storage)
- [BYD Battery-Box Premium HVM – Datenblatt](https://www.bydbatterybox.com/uploads/downloads/BCU2.0_BBOX_HVM_Datasheet_EN_V1.0_250328-67ee3a426f6ce.pdf)

## Aktueller Stand

- [x] .NET-10-Projekt und Visual-Studio-Solution
- [x] konfigurierbares Batteriemodell
- [x] 24-Stunden-Simulation mit Auto- und manuellem Modus
- [x] Pause, Änderungen und Fortsetzen während des Laufs
- [x] SOC-, Leistungs- und Temperaturgrenzen
- [x] auslösbare und rücksetzbare Übertemperaturstörung
- [x] Konsolenanzeige mit SOC-Balken
- [x] CSV-Verlauf und Entwicklungsjournal
- [x] erfolgreicher Build und vollständiger Probelauf
- [ ] aussagekräftige Screenshots für die Abgabe auswählen
