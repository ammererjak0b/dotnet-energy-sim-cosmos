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

## 2026-10-24 - Windkraft: Anlagenmodell, Energie und Sturmschutz

- **Done:** `WindPlant` und `WindPlantStatus` implementiert. Vereinfachte kubische
  Leistungskurve, Nennleistung, kumulierte Energie über virtuelle Zeitspannen,
  Sturmabschaltung mit Wiederanlaufgrenze, Wetterausfall, Ein/Aus, Leistungslimit,
  technische Störung und Neustart umgesetzt. Das Modell verwendet eine private
  Kopie der validierten Konfiguration und greift auf keine Uhr oder I/O zu.
- **KI:** Codex (GPT-6), agentische Umsetzung Verhalten für Verhalten mit roten
  xUnit-Tests, minimaler Implementierung und anschließender grüner Prüfung;
  Code, Zustandsübergänge und Ergebnis durch Diff, Build und Tests geprüft.
- **Artefact:** `src/EnergySim.Wind/WindPlant.cs`,
  `src/EnergySim.Wind/WindPlantStatus.cs`,
  `tests/EnergySim.Tests/Wind/WindPlantTests.cs`,
  `src/EnergySim.Wind/README.md`, `tests/EnergySim.Tests/README.md`.
- **Test:** 48 neue Modell-Testfälle und die bisherigen 39 Optionstestfälle grün,
  insgesamt 87 Wind-Testfälle. Gesamte Solution baut mit 0 Warnungen und 0 Fehlern.
  Gesamtlauf: 423 von 428 Testfällen grün; dieselben fünf bereits nachgewiesenen
  CSV-Dateisperrfehler wie im vorherigen Schritt, keine neuen fehlgeschlagenen Tests.
- **Comment:** Ausgangscommit `a808b23` auf `jonas`. Keine Änderung an bestehenden
  Simulatoren, Core, Projektverweisen oder CLI-Auswahl. Aktueller Windwert gilt im
  Lehrmodell als konstant über den übergebenen Schritt; keine Herstellerkennlinie,
  Rotorträgheit oder Umrechnung auf Nabenhöhe. Persönliche Erklärung und kompletter
  Übertragungsstand außerhalb des Repositories aktualisiert.
  - Next: `WindDevice` an den gemeinsamen Gerätevertrag anschließen und danach
    die Geräteauswahl in der CLI ergänzen.
