# Entwicklungsjournal Team (gemeinsame Arbeit)

## 2026-10-18 - Repo-Merge der drei LB01-Projekte

- **Beteiligte:** Jonas, Leon, Jakob
- **Done:**

  -  Lange Diskussion in Gruppencall über besten weg die 3 Repos zu überführen. 
  - Historien von PV, Batterie und Charger in `dotnet-energy-sim-cosmos` zusammengeführt
  - Jedes alte Projekt liegt unter `legacy/<name>/`, alle Commits mit Autor und Datum erhalten
  - Journale und `.claude` bleiben unverändert in `legacy/<name>/` (nichts verschoben oder gelöscht)
- **KI:** Claude Code (Sonnet 5.5)
  - Übernahm uns commit History aus den 3 Repos aus LB01.
- **Artefact:** `legacy/pv`, `legacy/battery`, `legacy/charger`, Tags `pv-lb01-2026`, `battery-lb01-2026`, `charger-lb01-2026`
- **Test:** Autoren und Commit-Zahlen stimmen (Leon 32, Jonas 3, Jakob 20)

## 2026-10-18 - Gemeinsames Grundgerüst (Core) aus den drei Simulatoren

- **Beteiligte:** Jonas, Leon, Jakob
- **Done:**
  - Nach langer Diskussion zu dritt mit Claude auf Projekt-Struktur geeinigt, Sektionen rausgesucht die sich doppeln, und mithilfe von Claude extrahiert und implementiert. 
  - Katastrophe: Dadurch dass wir Claude auch die Angabe gegeben haben und bei unsere Diskussion am Anfang nicht klar abgegrenzt haben, was genau wir von ihm wollen, bekamen wir erstmal ein 25 Assembly Projekt, dass eigentlich schon alles soweit implementiert hat. Dies wollten wir natürlich nicht, deswegen haben wir es komplett verworfen.
  - Neu: schlankes Grundgerüst mit 8 Projekten, erstmals wie am Anfang gewollt nur die drei LB01-Simulatoren
  - Gemeinsames Interface, eine Engine, eine Spectre-Konsole, ein CSV-Writer, kein doppelter Code
  - Jeder Simulator läuft einzeln über Kommandozeilen-Parametern: `dotnet run --project src/EnergySim.Cli -- --device pv|battery|charger`
- **KI:** Claude Code (Opus 5.5 für den verworfenen Entwurf, Sonnet 5.5 für das Grundgerüst [hatten dann keine tokens mehr xd]), Ergebnis geprüft durch Build, Unit Test und manuelle Testdurchläufe. Unit Tests wurden auch mithilfe von Claude geschrieben. Generell wurde bei diesem Step lange direkt mit der KI gearbeitet und diskutiert. Wir haben das ganze Agentisch umgesetzt.
- **Artefact:** `src/`, `tests/`, `EnergySim.sln`, `AGENTS.md`; 


| Projekt | Verweist auf | Inhalt |
|---|---|---|
| `Core` | nichts | Interface, Basisklasse, Engine, `LocalClock`, CSV, Wetter-Vertrag |
| `Weather` | Core | Wettermodell, Sonnenstand, Referenzjahr 2025 (3 Standorte), `LocalWeatherClient` |
| `Terminal` | Core | gemeinsame Spectre-UI, Log, Headless-Ausgabe |
| `Pv`, `Battery`, `Charger` | Core | je nur Modell, Optionen, Adapter |
| `Cli` | alle | Auswahl, Konfiguration, Verdrahtung |
| `Tests` | alle | xUnit |

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Journaleintrag Repo-Merge und Core)

## 2026-11-07 - LB02 Integrationsphase auf Branch teamarbeit

- **Beteiligte:** Jonas, Leon, Jakob
- **Done:**
  - Verträge geschrieben: Modbus-Register (`docs/contracts/modbus-registers.md`), Zeit, Wetter. Register-Map, Codec und Geräteprofile in `Core`
  - Geräte-Modbus-Server pro Prozess (Ports 1511-1514), schreibt Header und Gerätewerte, ungültige Writes werden zurückgesetzt (Warn-Bit 2)
  - Diagnose-Client `EnergySim.Diagnostic`: header, device, write, watch
  - Master: Teilnehmerstatus NEW/OK/UNREACHABLE, Skew aus Geräte-Uhr-Registern 18..27, Headless-Modus, Port-Regeln
  - Geräte verdrahtet: PV, Batterie, Charger, Wind liefern `SpecificValues`, Cli startet den Modbus-Server, Wind-Clock-Port 1604
  - Parallel gearbeitet: pro Modul ein Coding-Agent in eigenem Git-Worktree und Branch (`team/p2-modbus`, `p3-pv`, `p3-bat-wind`, `p3-charger`, `p4-diagnostic`, `p5-master`, `p9-docs`), danach nach `teamarbeit` gemerged
  - Ergebnis: Projektgraph ohne Zyklen, Startreihenfolge Master, Wetter, Geräte, Diagnose-Client in der README
  - Tests nachgezogen statt Test-first (Teamentscheidung, siehe `AGENTS.md`)
- **KI:** Claude Code (Sonnet 5.5), je ein Agent pro Modul. Jeder Agent bekam nur seine Dateien und den Vertrag. Ergebnis geprüft durch Diff-Review, Build, Unit Tests und manuelle Läufe mit mehreren Prozessen. Dokumentation ebenfalls von einem Agenten entworfen und von uns geprüft
- **Artefact:** `docs/contracts/`, `src/EnergySim.Modbus`, `src/EnergySim.Diagnostic`, `src/EnergySim.Time`, `src/EnergySim.Time.Cli`, `README.md`, `docs/architecture/modbus-virtual-time.md`
- **Test:** Build, Unit Tests und manuelle Läufe, Ergebnisse siehe `docs/evidence/README.md`

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Journaleintrag Integrationsphase)
