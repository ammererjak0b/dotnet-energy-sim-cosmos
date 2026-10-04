# Entwicklungsjournal - Jonas Kreibich

## 2026-10-02 - Projektstart

- **Done:** Startprojekt fuer den Batteriespeicher angelegt.
- **KI:** OpenAI Codex (GPT-5) zum Erstellen des Startgeruests verwendet.
- **Artefact:** `dotnet-energy-sim-lb01.sln`,
  `src/BatteryStorageSimulator/`, `README.md`, `data/` und `docs/`.
- **Test:** Build erfolgreich.
- **Comment:** 
	- Startprojekt angelegt. 
	- Next: Batterieparameter und Bedienkonzept festlegen.

## 2026-10-04 - Persoenliche Code-Erklaerung

- **Done:** Lokale Erklaerung des aktuellen Codes angelegt.
- **KI:** OpenAI Codex (GPT-5) zum Erstellen der Erklaerung verwendet.
- **Artefact:** `CODE-ERKLAERUNG.md`, `.gitignore` und `AGENTS.md`.
- **Test:** Git-Ignore-Regel erfolgreich geprueft.
- **Comment:**
	- Die Code-Erklaerung bleibt lokal und wird bei Codeaenderungen aktualisiert.
	- Next: Batterieparameter und Bedienkonzept festlegen.

## 2026-10-04 - Batteriespeicher-Simulation umgesetzt

- **Done:** Dynamische 24-Stunden-Simulation mit Auto- und manuellem Modus,
  Bedienmenue, SOC- und Leistungsgrenzen, Uebertemperaturstoerung und CSV-Ausgabe
  umgesetzt.
- **KI:** OpenAI Codex (GPT-5) fuer Strukturierung, Implementierung und Pruefung
  verwendet; die Vorschlaege wurden durch Build und Probelaeufe kontrolliert.
- **Artefact:** `src/BatteryStorageSimulator/`, `batterysettings.json`,
  `data/battery-run.csv`, `README.md` und `CODE-ERKLAERUNG.md`.
- **Test:** Build erfolgreich mit 0 Warnungen und 0 Fehlern; kompletter
  24-Stunden-Lauf, manuelles Laden und Entladen sowie Stoerung mit abgelehntem
  und erfolgreichem Reset geprueft.
- **Comment:**
	- Das Energiemodell nutzt feste Wirkungsgrade und ein vereinfachtes
      Temperaturmodell.
	- Next: Aussagekraeftige Konsolen-Screenshots fuer die Abgabe erstellen.

## 2026-10-04 - Konsolenanzeige geglaettet

- **Done:** Laufende Statusanzeige auf direktes Ueberschreiben fester Zeilen
  umgestellt; das staendige Leeren und sichtbare Flackern wurde entfernt.
- **KI:** OpenAI Codex (GPT-5) fuer Umsetzung und interaktive Pruefung der
  Konsolendarstellung verwendet; Menueverhalten nach dem Probebetrieb angepasst.
- **Artefact:** `Presentation/ConsoleUserInterface.cs`, `README.md` und
  `CODE-ERKLAERUNG.md`.
- **Test:** Build erfolgreich mit 0 Warnungen und 0 Fehlern; laufende Anzeige,
  Pause, Menue, Fortsetzen und Beenden interaktiv geprueft.
- **Comment:**
	- Beim Wechsel zum oder vom Menue wird die Konsole einmal neu aufgebaut;
      waehrend der laufenden Simulation werden nur die vorhandenen Zeilen
      aktualisiert.

## 2026-10-04 - Gesperrte CSV-Datei behandelt

- **Done:** Bei einer bereits geoeffneten CSV-Datei wird automatisch eine neue
  Ausgabedatei mit Zeitstempel angelegt, statt den Programmstart abzubrechen.
- **KI:** OpenAI Codex (GPT-5) fuer Fehleranalyse, Umsetzung und Pruefung der
  Ausweichdatei verwendet; Verhalten anhand einer realen Dateisperre geprueft.
- **Artefact:** `Persistence/CsvSimulationWriter.cs`, `README.md` und
  `CODE-ERKLAERUNG.md`.
- **Test:** Zugriff auf die gesperrte Standarddatei erkannt; Build und
  vollstaendiger Ausweichlauf anschliessend geprueft.
