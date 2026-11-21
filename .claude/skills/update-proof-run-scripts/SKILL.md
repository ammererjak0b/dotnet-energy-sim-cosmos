---
name: update-proof-run-scripts
description: Hält die EnergySim-Proof-Run-Skripte für Bash und PowerShell synchron und bindet neu hinzugekommene Geräte, Dienste, Gateways oder Viewer in den vollständigen Projektstart ein. Verwenden, wenn sich die ausführbaren Komponenten, Startparameter, Ports, Abhängigkeiten oder Nachweisschritte des Systems geändert haben.
---

# Proof-Run-Skripte aktualisieren

Verantwortlich: Jonas Kreibich

## Zweck

Aktualisiere diese beiden plattformspezifischen Skripte gemeinsam:

- `scripts/proof-run.sh` für Linux und Bash
- `scripts/proof-run.ps1` für Windows und PowerShell

Falls `scripts/proof-run.ps1` noch nicht existiert, erstelle es als funktional gleichwertiges Gegenstück zum Bash-Skript.

Nach der Aktualisierung müssen beide Skripte denselben vollständigen EnergySim-Proof-Run abbilden. Betriebssystemspezifische Prozess- und Pipe-Mechanismen dürfen unterschiedlich sein, das beobachtbare Verhalten muss jedoch übereinstimmen.

## Aufruf

```text
$update-proof-run-scripts```

## TODO (Jonas, fehlt für die Abgabe)

- **Kontext:** TODO welche Dateien gelesen werden
- **Schritte:** TODO
- **Grenzen:** TODO was der Skill nicht darf (kein Commit/Push, keine Secrets, ...)
- **Quelle/Lizenz:** TODO falls übernommen
- **Nachweis:** TODO Datum, Agent, Ergebnis, eigene Prüfung
