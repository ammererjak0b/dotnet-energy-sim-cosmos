# Entwicklungsjournal – Leon Angerer

## 2026-10-04 – Start Implementierung Dual-Port-Charger

- **Done:** 
    - Einrichtung Github Copilot 
    - Implementierung der Entitäten **DualPortCharger**, **ChargePort** und **ECar**
    - Implementierung erste Simulierung 
    - Einführung xUnit Tests
        - Connection Tests
        - Charging Tests
        - Snapshot Tests

- **KI:** Github Copilot mit GPT-5.6 Luna im "Plan" Modus Vorgehensweise und Architektur besprochen und im "Agent" Modus Kontinuierlich ausgearbeitet
- **Artefacts:**
### Grundmodell des Ladegeräts

- **Artefact:** `Domain/Entities/ECar.cs`, `Domain/Entities/ChargePort.cs`, `Domain/Entities/DualPortCharger.cs`
- **Artefact:** `LB_01.Tests/ChargePortConnectionTests.cs`
- **Artefact:** `LB_01.Tests/ChargingSimulationTests.cs`

### Verbindungs- und Trennlogik

- **Artefact:** `Domain/Entities/ECar.cs`
- **Artefact:** `Domain/Entities/ChargePort.cs`
- **Artefact:** `Domain/Entities/DualPortCharger.cs`
- **Artefact:** `LB_01.Tests/ChargePortConnectionTests.cs`

### Snapshots für die Anzeige

- **Artefact:** `Domain/Snapshots/ECarSnapshot.cs`
- **Artefact:** `Domain/Snapshots/ChargePortSnapshot.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `LB_01.Tests/ChargePortSnapshotTests.cs`

### Deterministische Simulation

- **Artefact:** `Domain/Simulation/Simulation.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `LB_01.Tests/SimulationTests.cs`
- **Artefact:** `LB_01.Tests/ChargingSimulationTests.cs`
- **Artefact:** `LB_01.Tests/SimulationAggregateTests.cs`

### Demo-Dashboard

- **Artefact:** `Application/ConsoleDashboard.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `Program.cs`
- **Artefact:** `LB_01.Tests/ConsoleDashboardTests.cs`

### Manuelle Simulationsteuerung

- **Artefact:** `Application/SimulationController.cs`
- **Artefact:** `Program.cs`
- **Artefact:** `LB_01.Tests/SimulationControllerTests.cs`

### Projektkonfiguration

- **Artefact:** `LB_01.csproj`
- **Artefact:** `LB_01.Tests/LB_01.Tests.csproj`
- **Artefact:** `LB_01.slnx`


Screenshot des Demo: Dashboards

![alt text](../../img/Dashboard_Demo.png)

## 2026-10-05 – Manuelle Ansteuerung der Simulation

- **Done:** 
    - Manuelle Steuerung & Konfiguration der Simulation
    - Parallelcharging

- **KI:** Github Copilot mit GPT-5.6 Luna im "Plan" Modus Vorgehensweise und Architektur besprochen und im "Agent" Modus Kontinuierlich ausgearbeitet

- **Artefacts:**
### Parallele Aufladung und faire Leistungsverteilung

- **Artefact:** `LB_01.Tests/ParallelChargingSimulationTests.cs`
- **Artefact:** `LB_01.Tests/ParallelChargingCompletionTests.cs`

### Konfiguration beim Programmstart

- **Artefact:** `Application/SimulationConfiguration.cs`
- **Artefact:** `Application/SimulationConfigurationDialog.cs`
- **Artefact:** `LB_01.Tests/SimulationConfigurationTests.cs`

### Laufzeitkonfiguration der Simulationsparameter

- **Artefact:** `Application/SimulationParameterConfiguration.cs`
- **Artefact:** `LB_01.Tests/SimulationParameterConfigurationTests.cs`
- **Artefact:** `LB_01.Tests/SimulationControllerConfigurationTests.cs`

## 2026-10-06 - Final Touches und CSV-Export
- **Done:** 
    - CSV-Export
    - Störungen 
    - Dynamische Fahrzeugerstellung
    - Simulationserweiterungen (Status, ...)

- **KI:** Github Copilot mit GPT-5.6 Luna im "Plan" Modus Vorgehensweise und Architektur besprochen und im "Agent" Modus Kontinuierlich ausgearbeitet

- **Artefacts:**
- ### SimulationSession und dynamische Fahrzeugverwaltung

- **Artefact:** `Application/SimulationSession.cs`
- **Artefact:** `Program.cs`
- **Artefact:** `LB_01.Tests/SimulationSessionTests.cs`

### CSV-Export von Simulationsdaten

- **Artefact:** `Application/CsvExporter.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `Domain/Snapshots/ChargePortSnapshot.cs`
- **Artefact:** `LB_01.Tests/CsvExporterTests.cs`

### Fahrzeugdaten im CSV-Export

- **Artefact:** `Application/CsvExporter.cs`
- **Artefact:** `LB_01.Tests/CsvExporterTests.cs`

### Charger- und Portstatus

- **Artefact:** `Domain/Entities/ChargerStatus.cs`
- **Artefact:** `Domain/Entities/ChargePortStatus.cs`
- **Artefact:** `Domain/Entities/DualPortCharger.cs`
- **Artefact:** `Domain/Entities/ChargePort.cs`
- **Artefact:** `Domain/Simulation/Simulation.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `Domain/Snapshots/ChargePortSnapshot.cs`
- **Artefact:** `LB_01.Tests/ChargerStatusTests.cs`

### Störungsbehandlung und CLI-Steuerung

- **Artefact:** `Program.cs`
- **Artefact:** `Application/ConsoleDashboard.cs`
- **Artefact:** `Domain/Entities/DualPortCharger.cs`
- **Artefact:** `Domain/Entities/ChargePort.cs`

### Erweiterte Status- und Fehlerdaten im CSV

- **Artefact:** `Application/CsvExporter.cs`
- **Artefact:** `LB_01.Tests/CsvExporterTests.cs`
- **Artefact:** `Domain/Snapshots/SimulationSnapshot.cs`
- **Artefact:** `Domain/Snapshots/ChargePortSnapshot.cs`

Ladekurve von 0% SOC auf 100% mit max 11kWh Ladeleistung 

![alt text](../../img/Ladekurve.png)

Screenshot des finalen Dashboards

![alt text](../../img/Final_Dashboard.png)

## 2026-10-25 - Gemeinsamer Zeitdienst

- **Done:**
    - Gemeinsame virtuelle Uhr mit monotonem Zeitanker, konfigurierbarer Geschwindigkeit, Start, Pause, Resume und Neustart
    - Modbus-TCP-Zeitdienst auf Loopback mit Geräteregistrierung, versionierten Zeitnachrichten und Heartbeats
    - Geräte können optional mit `--clock shared` die gemeinsame Uhr verwenden; lokale Uhr bleibt Standard
    - Heartbeat-Rennen behoben: Heartbeats für eine noch nicht empfangene Revision werden ignoriert
    - Fehlerhafte Statusbestätigungen entfernt und wiederholte Verbindungslogs reduziert
    - Neustart über `R` mit manueller Zeitangabe im Format `YYYY-MM-DD HH:MM` und lokaler Zeitzone
    - README um Hinweise zur Wiederverwendung durch einen zukünftigen Master-Simulator ergänzt
    - Zeitdienst und PV-Gerät lokal geprüft; Gerät blieb synchron und setzte einen Startbefehl um

- **KI:** OpenAI Codex; Architektur und Umsetzung iterativ ausgearbeitet; 
    - Modelle:
        - Plan: GPT-6 Astra
        - Implementation: GPT-5.6 Luna

- **Artefacts:**

### Gemeinsame virtuelle Uhr

- **Artefact:** `src/EnergySim.Core/Engine/ClockDefinition.cs`
- **Artefact:** `src/EnergySim.Core/Engine/ScheduledClock.cs`
- **Artefact:** `src/EnergySim.Core/Engine/ClockCursor.cs`
- **Artefact:** `src/EnergySim.Core/Engine/ClockSnapshot.cs`
- **Artefact:** `src/EnergySim.Core/Engine/IClockSource.cs`
- **Artefact:** `src/EnergySim.Core/Engine/DeviceSimulationLoop.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockDefinitionTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ScheduledClockTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockSafetyTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/SharedClockLoopTests.cs`

### Modbus-Kommunikation

- **Artefact:** `src/EnergySim.Modbus/ClockCodec.cs`
- **Artefact:** `src/EnergySim.Modbus/HeartbeatCodec.cs`
- **Artefact:** `src/EnergySim.Modbus/ClockIdentityCodec.cs`
- **Artefact:** `src/EnergySim.Modbus/ClockModbusClient.cs`
- **Artefact:** `src/EnergySim.Modbus/ClockModbusServer.cs`
- **Artefact:** `src/EnergySim.Modbus/ClockEndpointServer.cs`
- **Artefact:** `src/EnergySim.Modbus/SharedClockConnection.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockCodecTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/HeartbeatCodecTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockIdentityCodecTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockEndpointOptionsTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/ClockServiceInfoCodecTests.cs`

### Zeitdienst und Steuerung

- **Artefact:** `src/EnergySim.Time/TimeAuthority.cs`
- **Artefact:** `src/EnergySim.Time/ClockServiceHost.cs`
- **Artefact:** `src/EnergySim.Time/ClockAction.cs`
- **Artefact:** `src/EnergySim.Time/TimeServiceOptions.cs`
- **Artefact:** `src/EnergySim.Time.Cli/TimeServiceRunner.cs`
- **Artefact:** `src/EnergySim.Time.Cli/TimeServiceDashboardSource.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/TimeAuthorityTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Engine/TimeServiceDashboardSourceTests.cs`

### Geräte-CLI, Zeitneustart und Dokumentation

- **Artefact:** `src/EnergySim.Cli/CliRunner.cs`
- **Artefact:** `src/EnergySim.Cli/CliSettings.cs`
- **Artefact:** `src/EnergySim.Cli/ClockDashboardSource.cs`
- **Artefact:** `src/EnergySim.Terminal/DeviceDashboard.cs`
- **Artefact:** `src/EnergySim.Core/Devices/CommandDescriptor.cs`
- **Artefact:** `src/EnergySim.Core/Devices/DeviceCommand.cs`
- **Artefact:** `src/EnergySim.Core/Devices/DeviceSupport.cs`
- **Artefact:** `tests/EnergySim.Tests/Terminal/DeviceDashboardTests.cs`
- **Artefact:** `tests/EnergySim.Tests/Cli/CliSettingsTests.cs`
- **Artefact:** `tests/EnergySim.Tests/History/CsvHistoryWriterTests.cs`
- **Artefact:** `README.md`
- **Artefact:** `src/EnergySim.Time/README.md`
- **Artefact:** `src/EnergySim.Time.Cli/README.md`

- **Verifikation:** Solution-Build erfolgreich mit 0 Warnungen und 0 Fehlern; 382 Tests erfolgreich.

## 2026-10-25 – CSV-Export für den E-Auto-Lader

- **Done:**
    - Legacy-Spalten in die generische CSV-Pipeline integriert
    - Export einer Zeile pro neuer Simulationsaktualisierung
    - Standardpfad `data/export/charger_YYYY_MM_DD.csv` ergänzt; `--csv` bleibt verfügbar
    - CSV-Export bleibt vom Dashboard getrennt
- **Artefact:** `src/EnergySim.Charger/ChargerDevice.cs`, `src/EnergySim.Core/History/CsvHistoryWriter.cs`, `src/EnergySim.Cli/CliRunner.cs`
- **Artefact:** `tests/EnergySim.Tests/Charger/ChargerDisplayTests.cs`, `tests/EnergySim.Tests/History/CsvHistoryWriterTests.cs`
- **Artefact:** `src/EnergySim.Cli/README.md`
- **KI:** OpenAI Codex
- **Verifikation:** Charger- und CLI-Build erfolgreich mit jeweils 0 Warnungen und 0 Fehlern; 385 Tests erfolgreich. Ein paralleler Projektverweis-Build stieß auf eine Dateisperre in `EnergySim.Time.Cli`, daher wurden die betroffenen Projekte separat gebaut.

## 2026-10-27 – Dokumentation von Modbus und virtueller Zeit

- **Done:** Ablauf, Clock-Modell, Modbus-Register, Registrierung, Heartbeats und Integration in den Simulationsloop dokumentiert.
- **Artefact:** `docs/architecture/modbus-virtual-time.md`, `README.md`
- **KI:** OpenAI Codex
- **Verifikation:** Dokumentation mit den aktuellen Implementierungsdateien abgeglichen; keine Codeänderungen.
