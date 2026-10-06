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
![alt text](image.png)

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

![alt text](image-1.png)