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

## 2026-10-24 - Windkraft: Geräteadapter und gemeinsamer CLI-Start

- **Done:** `WindDevice` an den gemeinsamen Gerätevertrag angeschlossen.
  Gerätekopf in W/Wh, stabile Anzeige-/CSV-Spalten, Warnungen bei Wetterausfall
  und Sturm, technische Fehleranzeige sowie gemeinsame Befehle implementiert.
  `wind` in Gerätetyp, Factory, Auswahl, Hilfe und Visual-Studio-Startprofilen
  ergänzt. Windparameter werden aus dem gemeinsamen Abschnitt `Wind` gebunden.
- **KI:** Codex (GPT-6), agentische Umsetzung in xUnit-Rot/Grün-Schritten;
  Ergebnis mit Diff, vollständigem Build, automatisierten Tests und tatsächlichem
  lokalem CLI-Lauf einschließlich Wetter, CSV und kontrolliertem Beenden geprüft.
- **Artefact:** `src/EnergySim.Wind/WindDevice.cs`,
  `src/EnergySim.Core/Devices/DeviceType.cs`, `src/EnergySim.Core/README.md`,
  `src/EnergySim.Cli/DeviceFactory.cs`, `src/EnergySim.Cli/CliSettings.cs`,
  `src/EnergySim.Cli/CliRunner.cs`, `src/EnergySim.Cli/EnergySim.Cli.csproj`,
  `src/EnergySim.Cli/Properties/launchSettings.json`, `src/EnergySim.Cli/README.md`,
  `tests/EnergySim.Tests/Wind/WindDeviceTests.cs`,
  `tests/EnergySim.Tests/Cli/CliSettingsTests.cs`,
  `tests/EnergySim.Tests/Cli/DeviceFactoryTests.cs`,
  `tests/EnergySim.Tests/Cli/CliRunnerTests.cs`,
  `tests/EnergySim.Tests/Cli/ConsoleOutputCollection.cs`,
  `tests/EnergySim.Tests/Devices/DeviceCommandKeyTests.cs`,
  `src/EnergySim.Wind/README.md`, `tests/EnergySim.Tests/README.md`, `AGENTS.md`, `.gitignore`
  und `docs/evidence/wind-local-run.log`.
- **Test:** 110 Wind-Testfälle grün; 171 ausgewählte Wind-/CLI-/Gerätetestfälle grün.
  Gesamte Solution baut mit 0 Warnungen und 0 Fehlern. Gesamtlauf: 457 von 462
  Testfällen grün, dieselben fünf bekannten CSV-Dateisperrfehler. Lokaler CLI-Lauf:
  positive Leistung bis 23214 W, CSV-Zeilen mit wachsender Energie, Exitcode 0 nach
  `quit`; ungültige und nicht numerische Wind-Einstellungen verständlich abgelehnt
  mit Exitcode 2.
  Neues Wind-Startprofil ebenfalls erfolgreich gestartet und beendet.
- **Comment:** Ausgangscommit `d1cf721` auf `jonas`. Windmodell, Optionen, allgemeiner
  Loop, Wetterberechnung und CSV-Schreiber unverändert. Fehlendes Wetter und Sturm
  sind im gemeinsamen Kopf Standby mit sichtbarer Zusatzwarnung; technische
  Störungen verwenden Fehlercode 1. Persönliche Erklärung und vollständige
  Übertragungsdokumentation außerhalb des Repositories fortgeschrieben.
  Erzeugte CSV-Verläufe werden ignoriert; der kurze Laufnachweis bleibt versionierbar.
  - Next: Wind interaktiv ausprobieren; spätere Modbus-Anbindung und gemeinsame
    Registerkonventionen als eigenen Teamschritt festlegen.

## 2026-10-25 - Windkraft: Vorbereitung für den gemeinsamen Wetterprozess

- **Done:** Wettervertrag und gemeinsame HTTP-Verdrahtung aus `origin/jakob`
  bei `4b5f43e` ausschließlich gelesen. Die vorhandene Wind-Schnittstelle passt
  bereits dazu; keine Änderung an Windmodell, Adapter oder gemeinsamem
  Produktivcode nötig. Sechs Tests der echten Wind-/Loop-Kette für Wetterübergabe,
  virtuelle Energie, Ausfall, Stale und Wiederherstellung ergänzt. Startanleitung
  für den späteren gemeinsamen Stand und konkrete Merge-Hinweise dokumentiert.
- **KI:** Codex, Test- und Dokumentationsergänzung. Erster Nachweistest vor weiteren
  Änderungen ausgeführt und bereits grün; anschließend fünf zusätzliche Fälle.
  Kein roter Produktionstest und keine neue Produktivlogik, da das geforderte
  Verhalten bereits implementiert war. Testergebnisse und Umfang geprüft.
- **Artefact:** `tests/EnergySim.Tests/Wind/WindWeatherIntegrationTests.cs`,
  `tests/EnergySim.Tests/Wind/WindWeatherLoopHarness.cs`,
  `src/EnergySim.Wind/README.md`, `tests/EnergySim.Tests/README.md`,
  `docs/evidence/wind-weather-contract.md`.
- **Test:** Neue Integrationstests 6/6 grün; alle Wind-Testfälle 116/116 grün.
  Gesamte Solution baut mit 0 Warnungen und 0 Fehlern. Gesamtlauf: 463 von 468
  Testfällen grün, ausschließlich die fünf bekannten CSV-Dateisperrfehler.
  Die Integrationstests verwenden Fake-Wetter, Fake-Zeit und fertige Frames
  über einen Kanal, ohne echtes Netzwerk oder reale Wartezeiten.
- **Comment:** Ausgangscommit `18de2eb` auf `jonas`. Keine Übernahme, kein Merge,
  kein Commit/Push und keine Änderung an Jakobs Branch. Projektverweise und
  lokale Startprofile unverändert. HTTP-Client, Server und Wetter-URL stammen
  später aus Jakobs Beitrag auf `main`; echter HTTP-Lauf erst am gemeinsamen
  Stand. Persönlicher Plan, Erklärung und vollständige Rekonstruktion außerhalb
  des Repositorys fortgeschrieben. Vorherige Journaleinträge bleiben erhalten.
