# Batteriespeicher-Simulator - Aufgabe 01

Dieses Repository enthaelt aktuell nur ein lauffaehiges **Startprojekt** fuer die
.NET-10-Konsolenanwendung. Die fachliche Simulation ist bewusst noch nicht
implementiert.

## Starten

### In Visual Studio

1. `dotnet-energy-sim-lb01.sln` oeffnen.
2. `BatteryStorageSimulator` als Startprojekt auswaehlen.
3. Mit `F5` oder `Strg+F5` starten.

### Im Terminal

```powershell
dotnet run --project src/BatteryStorageSimulator
```

## Geplante Struktur

```text
dotnet-energy-sim-lb01/
|-- src/BatteryStorageSimulator/
|   |-- Configuration/   spaetere Einstellungen und Startparameter
|   |-- Domain/          fachliches Modell des Batteriespeichers
|   |-- Persistence/     spaetere CSV-Ausgabe
|   |-- Presentation/    spaetere Konsolenanzeige und Bedienung
|   |-- Simulation/      spaeterer Simulationsablauf und simulierte Zeit
|   `-- Program.cs       Einstiegspunkt, derzeit nur Startmeldung
|-- data/                spaetere CSV-Beispiellaeufe
|-- docs/
|   |-- development-journal/  persoenliches Entwicklungsjournal
|   `-- screenshots/           spaetere Konsolen-Screenshots
`-- dotnet-energy-sim-lb01.sln
```

Die Klassen in den Unterordnern sind nur leere Platzhalter. Sie zeigen die
beabsichtigte Aufteilung, enthalten aber noch keine Fachlogik.

## Anforderungen fuer die spaetere Umsetzung

- vollstaendiger simulierter Zeitraum von mindestens 24 Stunden
- Lade- und Entladeleistung manuell steuerbar und im Auto-Modus veraenderlich
- SOC aus Leistung und vergangener simulierter Zeit berechnen
- Kapazitaets- und Leistungsgrenzen beachten
- Auto-Modus pausieren, aendern und fortsetzen
- passende Stoerung ausloesen und zuruecksetzen
- Simulationsgeschwindigkeit und Schrittweite einstellbar machen
- Zeit, Modus, Leistung, kumulierte Energie und mindestens drei Zustandswerte anzeigen
- fuer mindestens einen kontinuierlichen Wert einen einfachen Balken anzeigen
- Verlauf in eine CSV-Datei schreiben
- Kapazitaet oder Nennleistung sowie Geraete-ID konfigurierbar machen

## Vor der Implementierung festzulegen

- Geraete-ID und Anzeigename
- nutzbare Kapazitaet in kWh
- maximale Lade- und Entladeleistung in kW
- Start-SOC sowie minimale und maximale SOC-Grenze
- Lade- und Entladewirkungsgrad
- Schrittweite der simulierten Zeit und gewuenschtes Tempo
- Verhalten des Auto-Modus
- Art der simulierbaren Stoerung
- Bedienkonzept: Menue, Kurzbefehle oder Kommandozeilenargumente
- CSV-Trennzeichen und Zahlenformat
- Vor- und Nachname fuer die Journaldatei

## Status

- [x] .NET-10-Konsolenprojekt und Visual-Studio-Solution angelegt
- [x] Grundstruktur angelegt
- [x] Start und Build pruefbar
- [ ] Fachmodell implementiert
- [ ] Simulation und Bedienung implementiert
- [ ] CSV, Tests, Screenshots und persoenliches Journal erstellt
