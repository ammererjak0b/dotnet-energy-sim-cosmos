# Projektregeln

## Entwicklungsjournal

- Bestehende Eintraege in `docs/development-journal/jonas-kreibich.md` bei
  neuer Arbeit niemals veraendern. Aenderungen an alten Eintraegen sind nur
  erlaubt, wenn Jonas sie ausdruecklich verlangt.
- Fuer neue sinnvolle Arbeitsschritte immer einen neuen datierten Eintrag am
  Dateiende anfuegen.
- `Done`, `KI` und `Artefact` sind Pflichtangaben.
- `Test` und `Comment` sind optional und werden nur aufgenommen, wenn sie fuer
  den jeweiligen Arbeitsschritt wirklich relevant sind.
- Bei `KI` Werkzeug, Modell, Einsatzform und den Umgang mit dem Ergebnis kurz
  nennen. Wenn keine KI verwendet wurde, ausdruecklich `keine` eintragen.
- Bei `Artefact` die konkret betroffenen Dateien, CSV-Dateien, Kurven,
  Screenshots, Dokumentation oder sonstigen Ergebnisse nennen.
- `Comment` kann Entscheidungen, Probleme, Erkenntnisse oder den naechsten
  Schritt enthalten. Keine Tests oder Kommentare erfinden, wenn es nichts
  Relevantes zu dokumentieren gibt.
- Eintraege kurz halten und grundsaetzlich diese Struktur verwenden:

```markdown
## YYYY-MM-DD - Kurzer Titel

- **Done:** Kurze Beschreibung des Ergebnisses.
- **KI:** Werkzeug, Modell, Einsatzform und Umgang mit dem Ergebnis; ohne KI: keine.
- **Artefact:** Betroffene Dateien oder Ordner.
- **Test:** Kurzes manuelles oder automatisiertes Testergebnis, falls relevant.
- **Comment:**
  - Entscheidung, Problem oder Erkenntnis, falls relevant.
  - Next: Naechster geplanter Schritt, falls relevant.
```

- Wenn `Next` verwendet wird, gehoert es als Unterpunkt zu `Comment` und ist
  kein eigener Hauptpunkt.

## Persoenliche Code-Erklaerung

- `CODE-ERKLAERUNG.md` erklaert Jonas den aktuellen Code in sehr einfacher
  Sprache und bleibt durch `.gitignore` ausschliesslich lokal.
- Nach jeder Aenderung am Programmcode, an der Projektstruktur oder an der
  Konfiguration muss `CODE-ERKLAERUNG.md` auf den neuen Stand gebracht werden.
- Die Erklaerung soll den Programmablauf, die Aufgabe jeder betroffenen Datei
  und neue C#-Begriffe schrittweise und ohne unnoetige Fachsprache beschreiben.

## Tests

- Fuer spaetere automatisierte Unit-Tests verwendet das Team xUnit.
- Kein anderes Unit-Test-Framework verwenden, sofern Jonas dies nicht
  ausdruecklich aendert.
- Fuer Aufgabe 01 nicht vorsorglich ein Testprojekt anlegen. Ein xUnit-Projekt
  wird erst hinzugefuegt, wenn es verlangt wird oder Jonas den Start der
  automatisierten Tests freigibt.
