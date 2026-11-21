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

## 2026-11-21 - LB04 WP5 Integrationstest

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Neues Projekt `tests/EnergySim.IntegrationTests` (in der `.sln`), getrennt von den Unit-Tests, Label `Category=Integration`
  - Schnittstelle gewählt: Geräte-Modbus-Server <-> Register-Client. Grund: so lesen Diagnostic-Client und Gateway, der Registerplan ist ein Vertrag mit festen Erwartungen
  - 4 Tests: Zustand vor dem ersten Frame (Typ, Map-Version, `NoMasterSync`), echtes PV-Modell mit festem Wetter (800 W/m2, 20 Grad) -> Header (3008 W, Running, Zeit, Sequenz 7, Uhr Running) und Gerätewerte (800, 400, 600 roh), Limit schreiben -> Befehl `SetLimit` 50 %, Limit über Bereich -> kein Befehl, Warn-Bit `InvalidWrite` bis zum nächsten gültigen Write
  - Test startet den Server selbst auf einem freien Port (vom OS), Timeouts (Start 10 s, Verbindung 2 s, Warten auf Write 5 s), Aufräumen in `DisposeAsync` auch nach Fehlschlag
  - Doku: `tests/README.md` (Voraussetzungen, Befehle, Ablauf, Wiederholen), Abschnitt in `test-and-evidence.md`, Anforderungen INT-1 bis INT-6, Rohnachweis `docs/evidence/lb04/wp5-integration-test.md`
- **KI:** Claude Code (Sonnet 5.5). Schnittstelle und Erwartungswerte aus Registerplan und PV-Formel abgeleitet (nicht aus dem Testlauf übernommen). Test absichtlich kaputt gemacht (3009 statt 3008): schlägt fehl, danach zurück. 8 Wiederholungen ohne Ausfall
- **Artefact:** `tests/EnergySim.IntegrationTests/`, `tests/README.md`, `docs/evidence/lb04/wp5-integration-test.md`
- **Test:** Build 0 Warnungen, 574 Unit + 4 Integrationstests grün, `--list-tests` listet 4, nach Fehlschlag kein offener Port. Offen: nur CLI geprüft, nicht in Rider/Visual Studio. Nur ein Geräteprofil (PV) über echten Socket, Gateway/RabbitMQ/Uhr-Endpunkte bleiben bei den manuellen Läufen

## 2026-11-21 - LB04 WP6 Konsolidierung

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Ist-Zustand geprüft statt umgebaut: Code, Tests, Doku, SRS, Skills. Ergebnis mit Problem, Änderung und Prüfung in `docs/technical-documentation/consolidation-lb04.md`
  - Problem 1: `Gateway`, `Messaging`, `Viewer` hatten keinen einzigen Test. Neu: 44 Unit-Tests (Abbildung Register -> Nachricht mit Qualitätsregeln, Gateway-Einstellungen, JSON, Routing, Broker-Einstellungen, Viewer-Text) und 5 Integrationstests (`GatewayDeviceTests` gegen einen echten Geräte-Modbus-Server, ohne Broker: neuer Stand, gleicher Stand = nichts Neues, neue Sequenz, falscher Typ = einmal Bad, Gerät weg = einmal Bad)
  - Problem 2: Viewer-Text steckte in einer privaten Methode mit `Console`. Jetzt reine Klasse `TelemetryFormat`
  - Problem 3: 4 Verstöße gegen die Stilregeln (Lambda, ternärer Operator, zwei Lambdas) behoben
  - Problem 4: Aufbau des Integrationstests (freier Port, Server, PV-Frame) in `PvServerHarness` ausgelagert, damit die zweite Testklasse nichts kopiert
  - Doku an den Ist-Zustand angepasst (README, Cli-README, Workflow, Tests-README, Zahlen nur noch in den Nachweisen), Anforderungen CON-1 bis CON-8 und Nachweise bei älteren Zeilen (MOD, FR-G, FR-V) nachgezogen, nichts gelöscht
  - Neuer Skill `check-srs-vs-code` (nur Bericht, ändert nichts ohne Bestätigung), einmal von Hand für dieses Protokoll ausgeführt. Owner offen (Teamentscheidung)
  - Offen aus der Prüfung: `AGENTS.md` Projektteil veraltet (WP7), Skill `update-development-journal` kennt nur Branch `teamarbeit` (Leon), Broker-Teile (`TelemetryConsumer`, `TelemetryPublisher`, `GatewayRunner`) nur manuell geprüft
- **KI:** Claude Code (Sonnet 5.5). Der Agent hat gesucht und vorgeschlagen, Änderungen kamen aus konkreten Funden (z.B. `grep` im Testordner zeigte, dass die drei Projekte nie referenziert wurden). Absichtlich kaputt gemacht: Qualitätsregel im Mapper, Test schlägt fehl, danach zurück. Verworfen: Fake für `ModbusRegisterClient` (würde den Fake testen, der echte Server ist einfacher und aussagekräftiger), Broker-Integrationstest mit RabbitMQ (nicht verlangt, Zusatz). Das Protokoll hat noch kein Teammitglied gegengelesen
- **Artefact:** `docs/technical-documentation/consolidation-lb04.md`, `docs/evidence/lb04/wp6-consolidation.md`, `src/EnergySim.Viewer/TelemetryFormat.cs`, `tests/EnergySim.UnitTests/{Gateway,Messaging,Viewer}/`, `tests/EnergySim.IntegrationTests/{GatewayDeviceTests,PvServerHarness}.cs`, `.agents/skills/check-srs-vs-code/`
- **Test:** Build 0 Warnungen, 618 Unit + 9 Integrationstests grün. Offen: nur CLI geprüft, Container und Gesamtlauf in WP6 nicht neu gelaufen

## 2026-11-21 - LB04 WP7 Projektanweisungen und Git-Regeln

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Code-Review über WP6 gelesen: 1 wichtiger Fund (Test "Gerät gestoppt" könnte flackern), danach 15 Wiederholungen ohne Ausfall. Funde und Entscheidung (noch nicht umgesetzt, WP7 vorgezogen) stehen in `docs/todo.md`
  - `AGENTS.md` aktualisiert: Aufbau (jetzt mit `Messaging`, `Gateway`, `Viewer`, Containern, zwei Testprojekten), Regeln (reine Klassen statt Konsolen-Logik, Beenden per SIGTERM), Befehle für Build, Test, Start und Docker, Doku-Karte mit Links statt Kopien
  - LB04-Testregel ergänzt (kein Test-First, erst Code, dann alle Tests, vor dem Merge grün), Definition of done an die Testregel angepasst (Fund aus dem Review von WP7 bei WP7 selbst: Regeln widersprachen sich)
  - Lambdas in Testcode per Regel erlauben: verworfen, das entscheidet das Team, nicht der Agent (offen in `docs/todo.md`)
  - Neuer Abschnitt "Git im Team": ein Branch pro Aufgabe, kleine Commits mit Pfaden, PR mit Prüfstand, `main` vor dem Merge in den Branch holen, Aufgabenwechsel nur mit Commit oder Stash, nie fremde oder ungesicherte Änderungen überschreiben (kein Force-Push, kein `reset --hard`, kein `stash drop` ohne Rückfrage), Agenten committen nur auf Anweisung
  - Tastenzeile korrigiert: `R` ist beim Master Neustart, bei den Geräten Fehlerreset
  - Anforderungen CON-6, GIT-1 bis GIT-4 und Aufgabenliste nachgezogen
- **KI:** Claude Code (Sonnet 5.5). Der Leitfaden "Git-Workflow im Team" lag dem Agenten nicht vor. Die Git-Regeln sind aus der Aufgabenstellung und unserer Arbeit in LB04 abgeleitet, nicht aus dem Leitfaden abgeschrieben. Das Team gleicht sie mit dem Leitfaden ab (Aufgabe in `docs/todo.md`).
- **Artefact:** `AGENTS.md`, `docs/technical-documentation/workflow.md`, `docs/requirements.md`, `docs/todo.md`
- **Review-Fixes:** Code-Review über WP7: kein wichtiger Fund, 6 Nits. Angenommen: Skriptzeile war eine Pipe (jetzt getrennte Zeilen, plus `proof-run.sh`), Branchliste, widersprüchliche Testregeln (Abschnittsüberschrift sagt jetzt, dass TDD nur für die LB02-Basis gilt), falscher WP-Bezug im Journal, `GIT-2`/`CON-6` mit Hinweis auf den offenen Abgleich mit dem Leitfaden. Nicht angefasst: `jakob-ammerer.md` (persönliches Journal, Formatfehler in der unfertigen Änderung meldet das Review, Jakob prüft selbst)
- **Test:** keine Codeänderung. Befehle der Befehlsliste gegen die Projekte gelesen (Pfade, Optionen). `git status` zeigt nur die genannten Dateien

## 2026-11-21 - LB04 Review-Funde WP5 bis WP7 abgearbeitet

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Alle offenen Funde der Code-Reviews zu WP5, WP6 und WP7 in einem Zug auf Branch `lb04/review-fixes`
  - Test "Gerät gestoppt": prüft jetzt, dass keine Messwerte und keine virtuelle Zeit mehr gesendet werden. Vorher 15 Wiederholungen ohne Ausfall (Fund "könnte flackern" war nicht nachweisbar)
  - Gültiger Write: Limit-Register wird zurückgelesen (500), abgelehnter Write: alter Wert (1000) ist wieder da. Dafür neue Methode `ModbusRegisterClient.readHoldingRegisters`
  - Warten auf den Header schlägt bei Zeitüberschreitung mit Text fehl statt einen alten Header zurückzugeben, Geräteblock liest nur noch die 3 geprüften Werte
  - `PvServerTestBase` statt doppeltem Zugriffs- und Aufräumcode in den zwei Integrationsklassen, `PvServerHarness` versucht es bei belegtem Port bis zu 3 Mal
  - `TelemetryFormat` verträgt `null` bei `warnFlags` und `measurements` (Test schlägt ohne den Schutz mit `ArgumentNullException` fehl)
  - Alle 31 Lambdas im Testcode in `delegate`-Form umgebaut, damit die Definition of done ohne Ausnahme gilt. Die offene Teamentscheidung dazu entfällt
  - Nicht angefasst: persönliches Journal `jakob-ammerer.md` (Formatfehler der unfertigen Änderung meldet das Review, die Person prüft selbst), Abgleich der Git-Regeln mit dem Moodle-Leitfaden (Leitfaden liegt dem Agenten nicht vor), IDE-Prüfung und Windows/macOS (brauchen Menschen am Rechner)
- **KI:** Claude Code (Sonnet 5.5). Jeder Fund einzeln gegen den Code geprüft, der Fund zum Flackern widerlegt durch Wiederholung statt blind geändert. Verworfen: Retry im Server selbst (Testproblem, kein Produktproblem)
- **Artefact:** `src/EnergySim.Modbus/ModbusRegisterClient.cs`, `src/EnergySim.Viewer/TelemetryFormat.cs`, `tests/EnergySim.IntegrationTests/{PvServerTestBase,PvServerHarness,DeviceModbusServerTests,GatewayDeviceTests}.cs`, `tests/EnergySim.UnitTests/**`
- **Test:** Build 0 Warnungen, 619 Unit + 9 Integrationstests grün, Integration 3 Mal wiederholt ohne Ausfall

## 2026-11-21 - LB04 Skripte, Portainer, Projekt-READMEs, Präsentationsanleitung

- **Beteiligte:** Jonas, Leon, Jakob (Gruppencall), Agent-Lauf über Jakobs Rechner
- **Done:**
  - Skripte neu für Linux (bash) und Windows (PowerShell), je ein Paar: `run-simulation` (Gesamtlauf in eigenen Fenstern, Modi `native`, `pv2`, `docker`, `stop`) und `proof-run` (automatischer Lauf ohne Fenster, neu mit Single-Step-Schritt). Alte `testrun_task2.sh` und `testrun_task3.sh` gelöscht, weil sie nur LB02/LB03 kannten und kein Windows-Gegenstück hatten
  - `proof-run.ps1` nutzt `System.Diagnostics.Process` und eine kleine C#-Klasse für die Logs (PowerShell hat keine benannten Pipes wie die bash-fifos). Unterschied: unter Windows wird der Wetter-Prozess per Kill beendet (kein Ctrl+C an Hintergrundprozesse), alle anderen enden mit `quit` und Exit-Code 0
  - Geprüft: `proof-run.sh` real gelaufen (alle Exit-Codes 0, Single Step +12 s je Schritt im Header), `proof-run.ps1` mit PowerShell 7 im Container auf Linux real gelaufen (gleiche Schritte, +12 s je Schritt), `run-simulation.sh` im Modus `docker` und `pv2` real gestartet und mit `stop` beendet, `run-simulation.ps1` nur geparst und Fenster-Befehl geprüft. Auf einem echten Windows nicht gelaufen (offen in `docs/todo.md`)
  - Portainer als Compose-Dienst (`portainer/portainer-ce:2.21.5`, Port 9443 nur auf `127.0.0.1`, Admin-Passwort legt man beim ersten Besuch selbst an), Anleitung in `docker.md`. Gestartet, healthy, HTTP 200. Docker-Socket-Mount ist root-gleich, im Doku vermerkt
  - Jedes Projekt-README (16 src, 2 tests) beginnt jetzt mit derselben kurzen Tabelle: Zweck in Stichworten, Typ, benutzt, benutzt von, Owner, Start, Tests, Doku. Der alte lange Text bleibt darunter (Registerpläne, auf die Verträge verlinken)
  - Neue Anleitung `docs/user-documentation/presentation.md`: Vorbereitung, Teil A (4 Geräte nativ), Teil B (Container), Tests zeigen, Nachweise, Fehlerliste
  - Doku nachgezogen: README (Skripte-Tabelle, Portainer), `AGENTS.md` (Befehle), Anforderungen (SYS-8, CTR-12, AC-9), Entscheidungen, Test-und-Nachweis, Benutzer-Doku, Aufgabenliste, Nachweis `docs/evidence/lb04/scripts.md`
- **KI:** Claude Code (Sonnet 5.5). Das PowerShell-Skript hat der Agent geschrieben und auf Linux im Container geprüft, nicht auf Windows. Verworfen: PowerShell-Skript mit `Register-ObjectEvent` für die Logs (Ereignisse laufen nur im Leerlauf der Pipeline, zu unzuverlässig, deshalb die C#-Klasse), PowerShell als dotnet-Tool installieren (Tool ist kaputt, das SDK-Image bringt `pwsh` schon mit)
- **Artefact:** `scripts/{run-simulation,proof-run}.{sh,ps1}`, `compose.yaml`, `src/*/README.md`, `tests/*/README.md`, `docs/user-documentation/{presentation,docker}.md`, `docs/evidence/lb04/scripts.md`
- **Test:** Build 0 Warnungen, 619 Unit + 9 Integrationstests grün (unverändert), Läufe wie oben. Offen: Windows

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Journaleinträge LB04 WP1 bis WP7, Review-Funde, Skripte)
