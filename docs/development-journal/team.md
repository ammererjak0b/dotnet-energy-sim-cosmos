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

## 2026-11-08 - LB03 Modbus zu RabbitMQ

- **Beteiligte:** Jakob (Agent-Lauf), Team prüft
- **Done:**
  - Plan mit Agent besprochen: AMQP 0-9-1 statt MQTT/AMQP 1.0, ein Gateway, Branch `lb03`, kein TDD
  - SRS, Nachrichtenvertrag, Compose (`rabbitmq:4.3.7-management`, `.env`)
  - `HeaderSnapshot` nach `Modbus`, neue Projekte `Messaging`, `Gateway`, `Viewer`
  - Lauf: 4 Geräte im Viewer, Broker stop/start, Gerät beendet -> `Bad`
- **KI:** Claude Code (Sonnet 5.5), Plan vom Team geändert (Struktur wie Diagnostic, kompaktes Journal, kein TDD)
- **Artefact:** `docs/SRS-LB03.md`, `docs/contracts/messaging.md`, `compose.yaml`, `src/EnergySim.{Messaging,Gateway,Viewer}`, `scripts/testrun_task3.sh`
- **Test:** Build 0 Warnungen, 529 Tests grün, Lauf mit Logs. Offen: Pause-Test, Evidence-Screenshots, Owner Gateway/Viewer/Messaging, Skills

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Journaleinträge Integrationsphase, LB03)

## 2026-11-21 - LB04 WP1 Dokumentation und Anforderungen, WP2 Single Step

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall, ein Rechner), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Aufgabe LB04 mit dem Agent im Plan-Modus besprochen, Plan in 8 Pakete (WP1 bis WP8) zerlegt, jedes Paket ein Ticket: Agent arbeitet, Team prüft, Team committet und pusht
  - Entscheidungen: Loopback-Grenze der Zeit-Endpunkte lockern (Standard bleibt `127.0.0.1`), Master wählt Gerät über Hostname = Geräte-Id, kein Gateway-Container, Single Step = ein Geräte-Tick (200 ms real x Tempo), Container: Jakob WeatherSim, Jonas Wind, Leon Master, Tests in Unit- und Integrationsprojekt trennen, kein Test-First (erst Code, dann alle Tests, dann laufen lassen), keine FakeItEasy (handgeschriebene Fakes)
  - WP1: `docs/` als Obsidian-Vault, `docs/requirements.md` (SRS-LB03 hineingezogen, LB04-Anforderungen mit Status), `docs/technical-documentation/`, `docs/user-documentation/`, `docs/todo.md` als gemeinsame Aufgabenliste (Lehrer und Team), Entscheidungen in `decisions.md`
  - WP2: Master-Befehl Single Step (Taste `S`, headless `step`), nur im Zustand Paused. Ein Schritt ist eine einzelne Uhr-Definition: Status bleibt Paused, virtuelle Zeit springt um 200 ms x Tempo vor. Geräte-Loop zählt den Vorwärtssprung als einen Schritt (Schritt = Differenz zweier Uhrablesungen, jetzt in jedem Zustand statt nur bei Running)
  - Verworfen: Resume + Pause als zwei Änderungen (nur eine Änderung gleichzeitig offen, Revision muss +1 sein, zu fragil)
  - Kontrakt `docs/contracts/time.md`, READMEs und User-Doku nachgezogen, Journal und Aufgabenliste aktualisiert
  - Code-Review-Agent über den Commit laufen lassen: ein wichtiger Fund (Schleife zählte jeden Vorwärtssprung ohne Running als Schritt) -> angenommen, Sprung bei Pause zählt nur bis 200 ms x Tempo, größere werden ignoriert (Warnung). Nits angenommen: Test prüft `pending()` nach Ablehnung, Doku zu Lead-Zeit und fester Schrittgröße. Verworfen: Schritt ablehnen über `MaxStepSeconds` (Tempo max. 3600, Schritt max. 720 s, kann nicht eintreten), Fund zu `jakob-ammerer.md` (nicht unsere Änderung)
- **KI:** Claude Code (Sonnet 5.5), Plan-Modus mit Rückfragen, Team hat den Plan zweimal geändert (kein Test-First, Gateway-Container raus, Hostname = Id, Doku-Pflicht je Paket). Code gelesen und Entwurf geprüft: Schleife und Uhr-Code nachgelesen, bevor der Sprung-Ansatz gewählt wurde. Ergebnis geprüft durch Build, Tests und manuellen Lauf
- **Artefact:** `docs/requirements.md`, `docs/todo.md`, `docs/technical-documentation/`, `docs/user-documentation/`, `src/EnergySim.Time/{ClockAction,TimeAuthority}.cs`, `src/EnergySim.Time.Cli/`, `src/EnergySim.Core/Engine/DeviceSimulationLoop.cs`, `tests/EnergySim.Tests/Engine/{TimeAuthorityTests,DeviceSimulationLoopTests}.cs`
- **Test:** Build 0 Warnungen, 539 Tests grün (10 neu: Step, Step x2, Step bei Tempo 120, Step bei Stopped/Running abgelehnt, Step bei offener Änderung abgelehnt, Pause + Resume stetig, Start aus Running abgelehnt, Loop mit Sprung bei Pause, Loop ignoriert zu großen Sprung bei Pause). Manueller Lauf: Master headless + Batterie-Gerät, `start`, `pause`, `step` x3, `resume`, `step` bei Running abgelehnt. Revision stieg je Schritt (4, 5, 6), Gerät empfing Revision 6. Grenze: die Anzeige hat Minutenauflösung, 12 s je Schritt sind dort nicht sichtbar

## 2026-11-21 - LB04 WP3 Unit-Tests aufräumen

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Testprojekt `tests/EnergySim.Tests` in `tests/EnergySim.UnitTests` umbenannt (Ordner, csproj, Namespaces, `.sln`, READMEs, Doku). Journale der Teammitglieder bleiben unverändert (alte Pfade = Historie)
  - Integrationsprojekt wird erst in WP5 mit dem ersten Test angelegt (leeres Projekt mit 0 Tests wäre kein Nachweis)
  - Bestehende Modelltests gegen die Pflichtbeispiele geprüft: PV aus = 0 W, Batterie-Energiegrenzen, Ladeleistungsgrenze, Wind Cut-in/Sturm sind abgedeckt. Lücke: Reaktion der PV auf geänderte Einstrahlung (nur ein Punkt bei 800 W/m2)
  - Neu: PV-Theory für 0, 200, 400, 800, 1000 W/m2 bei 20 Grad (Erwartung von Hand aus der Formel gerechnet, Toleranz 6 Nachkommastellen) und ein Test: halbe Einstrahlung gibt mehr als halbe Leistung, weil die Zelle kühler bleibt
  - `WindWeatherIntegrationTests` bleibt bei den Unit-Tests: echtes Gerät + echte Schleife, aber Fake-Wetter und Fake-Uhr, keine echte Schnittstelle. Zählt nicht als Integrationstest für LB04
  - Doku: Testauswahl mit Begründung je Bereich (`test-and-evidence.md`), Befehle inkl. `--list-tests`, Anforderungen TST-1 bis TST-7 und Aufgabenliste aktualisiert
- **KI:** Claude Code (Sonnet 5.5). Umbenennung per `git mv` + `sed`, danach Build und alle Tests. Erwartungswerte der neuen PV-Tests selbst aus der Formel in der PV-README nachgerechnet, nicht aus dem Code übernommen
- **Artefact:** `tests/EnergySim.UnitTests/`, `tests/EnergySim.UnitTests/Pv/PvPlantTests.cs`, `tests/EnergySim.UnitTests/README.md`, `docs/technical-documentation/test-and-evidence.md`, `EnergySim.sln`
- **Skills:** Skill `i-have-adhd` (externes Plugin, MIT, Version 0.4.1) unverändert in `.agents/skills/` und `.claude/skills/` kopiert, Zeile in der Skill-Tabelle in `AGENTS.md` (Owner Jakob). Nur `SKILL.md`, die Gemini/OpenAI-Dateien des Plugins nicht übernommen. Läuft nur auf ausdrücklichen Aufruf (`disable-model-invocation`), ändert keinen Code. Hat nicht das Feld-Schema der eigenen Skills (Owner, Purpose, Steps), Inhalt bewusst nicht verändert wegen Lizenz und Updates
- **Test:** Build 0 Warnungen, 545 Tests grün (6 neu), `dotnet test tests/EnergySim.UnitTests --list-tests` listet die Tests. Offen: einmal in Rider/Visual Studio prüfen, ob die Tests dort gefunden werden (nur über CLI geprüft)

## 2026-11-21 - LB04 WP4 Container

- **Beteiligte:** Jonas (Wind), Leon (Master), Jakob (WeatherSim), Gruppencall, alles auf einem Rechner, Agent-Lauf über Jakobs Rechner
- **Done:**
  - Problem: Uhr-Endpunkte waren an 4 Stellen fest auf `127.0.0.1`, Container im Docker-Netz konnten sich nicht erreichen
  - Bind-Adresse und Master-Host per Option (`--host`, `--clock-host`, `--time-host`), Standard bleibt Loopback
  - Zweites Problem: der Master erfährt die Geräte-Id erst nach dem Zurückrufen, braucht den Namen aber vorher. EasyModbus liefert die Client-IP bei einem Write nicht (per Reflection nachgesehen). Entscheidung: Registrierung schickt Port + Geräte-Id (19 Register), alte 1-Register-Form bleibt gültig. Master ruft mit `--dial-device-id` die Id als Hostnamen (Compose-Servicename = Id)
  - `--autostart` am Master für unbeaufsichtigten Lauf
  - Drei Dockerfiles (Multi-Stage, `sdk:10.0` zum Bauen, `runtime:10.0` zum Laufen), `.dockerignore`, `compose.yaml` erweitert (Bridge-Netz, Healthchecks, `depends_on`, `stdin_open`), Ports nur auf `127.0.0.1`
  - Zuordnung: Leon Master, Jakob WeatherSim, Jonas Wind. Kein Gateway-Container
  - Beim Stoppen Fehler gefunden: `wind-1` endete mit Exit-Code 137. Ursache: im Headless-Modus blockiert `Console.In` synchron, Ctrl+C und SIGTERM beendeten das Gerät bei offenem stdin nie (alter Fehler, auch bei Ctrl+C). Fix: Leser in `Task.Run`, SIGTERM wie Ctrl+C, Unit-Test dazu (hängt ohne Fix). Danach Exit-Code 0 in 0,6 s
  - Doku: `docs/user-documentation/docker.md` (Bauen, Starten, Status, Logs, Eingreifen, Stoppen, Fehlersuche), README-Tabelle Person/Simulator/Container, Kontrakt `time.md`, READMEs, Architektur, Entscheidungen, Anforderungen CTR-1 bis CTR-11, Rohnachweis `docs/evidence/lb04/wp4-containers.md`
  - Code-Review-Agent über den Commit: 2 wichtige Funde, 5 Nits. Angenommen: Teilnehmer nach Host:Port statt nur Port (zwei Wind-Container mit gleichem Standard-Clock-Port hätten sich gegenseitig ersetzt), Compose-Kommentar zu Port 1502 korrigiert (native Geräte können dem Container-Master nicht beitreten, Grenze steht in `docker.md`), Warnzeile bei Bind auf nicht-Loopback und Vertrauensannahme dokumentiert, `--autostart` mit Fehlerbehandlung, Stil (`delegate`, Keyword-Kommentar), eine Host-Regel (`ClockEndpointOptions.validateHost`) statt zwei. Verworfen: Opt-in-Flag für Bind auf `0.0.0.0` (zu viel für ein Teamnetz, Standard bleibt Loopback, Warnung reicht), Allow-Liste für Gerätenamen (siehe Vertrauensannahme)
- **KI:** Claude Code (Sonnet 5.5). Plan vom Team vorgegeben (Hostname = Id, kein Gateway-Container, Doku-Pflicht). Der Agent hat die Registrierung geändert, weil der Plan ein Henne-Ei-Problem hatte (Id vor dem Rückruf nötig), Team-Entscheidung "Id als Hostname" bleibt. Ergebnis geprüft durch Build, Tests und echten Containerlauf (Master, Wetter, Wind, Diagnostic, Gateway, Viewer, `docker attach`, `docker compose stop`)
- **Artefact:** `compose.yaml`, `.dockerignore`, `src/EnergySim.{Time.Cli,WeatherService,Cli}/Dockerfile`, `src/EnergySim.Modbus/ClockRegistration*.cs`, `src/EnergySim.Time/ClockServiceHost.cs`, `src/EnergySim.Cli/HeadlessRunner.cs`, `docs/user-documentation/docker.md`, `docs/evidence/lb04/wp4-containers.md`
- **Test:** Build 0 Warnungen, 574 Tests grün (29 neu). Manuell: Container gebaut und gestartet, `docker compose ps` healthy, Master zeigt `Registered wind-1 on wind-1:1604`, Wetter-Abfrage per curl und durch wind-1, Diagnostic `write limit 50` (4841 W auf 4542 W), Wind Modbus -> Gateway -> RabbitMQ -> Viewer, `pause`/`step`/`step`/`resume` über `docker attach`, Stoppen Exit-Code 0. Nach den Review-Fixes Container neu gebaut und gestartet: Registrierung `wind-1 @ 1604`, Stoppen Exit-Code 0. Offen: zwei Wind-Container nur als Unit-Test des Schlüssels, nicht in Docker gelaufen. Windows/macOS nicht geprüft, jeder baut und startet seinen Container einmal selbst (WP8), `docker attach` per Pipe in einer Zeile hängt (mit `timeout -s KILL` gelöst)

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Journaleinträge LB04 WP1 bis WP4)
