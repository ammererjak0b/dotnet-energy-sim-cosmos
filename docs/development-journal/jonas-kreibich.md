# Entwicklungsjournal Jonas Kreibich - Aufgabe 02

## 2026-10-24 - Windkraft: Konfiguration und Validierung

- **Done:** Neues Class-Library-Projekt `EnergySim.Wind` mit `WindOptions` angelegt.
  Demonstrationswerte für Nennleistung und Windgrenzen sowie deren Validierung
  implementiert. Windprojekt in Solution und gemeinsamem Testprojekt eingebunden.
  Leistungskurve, Adapter und CLI-Geräteauswahl folgen in späteren Schritten.
- **KI:** Codex (GPT-6), agentische Umsetzung in kleinen xUnit-Rot/Grün-Schritten;
  generierten Code und Projektänderungen durch Diff, Build und Tests geprüft.
- **Artefact:** `src/EnergySim.Wind/EnergySim.Wind.csproj`,
  `src/EnergySim.Wind/WindOptions.cs`, `src/EnergySim.Wind/README.md`,
  `tests/EnergySim.Tests/Wind/WindOptionsTests.cs`,
  `tests/EnergySim.Tests/README.md`, `tests/EnergySim.Tests/EnergySim.Tests.csproj`,
  `EnergySim.sln`.
- **Test:** Rot/Grün für Standardwerte und jede Validierungsregel durchgeführt.
  Alle 39 neuen Wind-Testfälle grün. Gesamte Solution baut mit 0 Warnungen und
  0 Fehlern. Gesamtlauf: 375 von 380 Testfällen grün; fünf bestehende CSV-Tests
  scheitern unter Windows an Dateisperren. Gegenprüfung am unveränderten
  Ausgangscommit `5e10b95`: dieselben fünf Fehler, 336 von 341 Testfällen grün.
- **Comment:** Nur Solution und Test-Projektverweis an bestehenden Dateien
  angepasst. Vorhandene Simulatorlogik bleibt unverändert. Persönliche Erklärung
  und vollständige Übertragungsdokumentation liegen außerhalb des Repositories.
  Die bestehenden CSV-Fehler wurden in diesem Wind-Schritt nicht geändert.
