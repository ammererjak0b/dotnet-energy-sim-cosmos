# AGENTS.md - Coding Style (C# / .NET)

Generic rules for every coding agent (Codex, Copilot, Claude, ...) and every team member.
Readability over cleverness. Write code that can be read top to bottom without decoding.

## Workflow: Test Driven Development

- Test first, always: red -> green -> refactor
  1. Write one failing xUnit test for the next small behavior
  2. Run it, see it fail for the right reason
  3. Write the minimum code to pass
  4. Refactor with all tests green
- No production code without a failing test that demands it
- One behavior per test, small steps, commit when green
- Framework: xUnit (`[Fact]`, `[Theory]` with `[InlineData]` for value tables)
- Test name: `MethodName_Condition_ExpectedResult`, e.g. `Advance_WhenPaused_KeepsEnergyUnchanged`
- Body in three blocks marked with `// Arrange`, `// Act`, `// Assert`
- Prefer hand-written fakes over mocking frameworks; inject time (`TimeProvider`) instead of reading the clock directly
- Tests must be deterministic: no real network, no real clock, no `Thread.Sleep`, seeded randomness only
- Bug found -> write the failing test that reproduces it first, then fix
- Before handing work back: build and run all tests, report the result honestly
- LB02 Integrationsphase auf Branch teamarbeit: Tests nachgezogen statt Test-first (Teamentscheidung).
- LB03: kein Test-first, Teamentscheidung. Bestehende Tests bleiben grün.
- LB04: kein Test-first, Teamentscheidung. Erst der Code, dann alle zugehörigen Tests schreiben, dann laufen lassen und beheben. Vor dem Merge alles grün. Unit-Tests (`tests/EnergySim.UnitTests`) ohne Netzwerk und laufenden Dienst, Tests mit echter Schnittstelle in `tests/EnergySim.IntegrationTests`. Neue Tests mit festen Erwartungen aus Anforderung, Vertrag oder Formel, nicht aus dem Lauf übernommen

## Naming

| Element | Style | Example |
|---|---|---|
| Local variables, parameters | camelCase | `chargePowerKw` |
| Methods | camelCase | `calculateChargePower()` |
| Properties | PascalCase | `StoredEnergyKwh` |
| Private fields | `_camelCase` | `_weatherClient` |
| Constants (`const`, constant-like `static readonly`) | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |
| Types (class, record, struct, enum, interface), namespaces | PascalCase (interfaces with `I`) | `BatteryStorage`, `IWeatherClient` |
| Async methods | camelCase with `Async` suffix | `loadDataAsync(ct)` |

- Speaking names: from the name alone you must know what it does and which unit it has (`socPercent`, `stepMinutes`)
- No abbreviations except well known ones (`id`, `kw`, `soc`)
- No suffixes like `_v2`, `_final`, `_old`, `_backup`; git keeps the history

## No inline ifs, no syntactic sugar

Always the plain, explicit form. A reader must understand each line instantly.

Forbidden, with the allowed replacement:

| Forbidden | Write instead |
|---|---|
| ternary `a ? b : c` | `if` / `else` block |
| `if (x) return;` without braces, or `if (x) { ... }` on one line | braces always, body on its own lines |
| `??`, `??=`, `?.`, `?[]` | explicit `if (x == null)` check |
| expression-bodied members `=> expr` | normal body with `{ return expr; }` |
| `switch` expressions, `is` / `and` / `or` patterns | classic `switch` statement or `if` chain |
| inline `for` tricks, `for` over collections | `foreach` for collections; `for` only when the index itself is needed |
| `var` when the type is not obvious | explicit type (`double powerKw = ...`) |
| target-typed `new()` | `new BatteryStorage(...)` |
| collection expressions `[1, 2]`, spread `..` | `new List<int> { 1, 2 }` |
| primary constructors | normal constructor with field assignment |
| tuple deconstruction, local functions tricks | small named methods and small result classes |
| LINQ chains and query syntax | plain loops; a single trivial call (`Sum`, `Any`) is ok |

Example:

```csharp
// forbidden
double power = isOn ? Math.Min(requestKw, MAX_POWER_KW) : 0;

// required
double power = 0;
if (isOn)
{
    power = Math.Min(requestKw, MAX_POWER_KW);
}
```

## Structure

- One class per file, file name = type name
- Small methods with one job; if a method needs a comment block to be understood, split it
- Guard clauses first (validate arguments), then the work
- No magic numbers: name them as constants
- No `async void` (except event handlers), no `.Wait()`, `.Result`, `Thread.Sleep` in async code
- I/O is async (`Async` suffix, `CancellationToken` as last parameter)
- No empty `catch`; catch only what you can handle, log or surface the rest
- Separate model (pure logic), flow, console, communication and configuration into their own classes or assemblies; the model never references UI or I/O
- No duplicated code across projects: shared code goes into a shared class library (KISS, DRY)
- No interface with a single implementation unless a second one is planned in the same change
- Dependencies point downwards only, no project reference cycles

## Comments

- Inline comments next to the code line, short keywords, no sentences
- Exactly one space before `//`, never column-aligned
- Describe classes and functions in a short summary above them
- Comment what is not obvious from names (units, formulas, reasons); do not comment the obvious

```csharp
double energyKwh = powerKw * step.TotalHours; // kW * h
```

## Repo hygiene

- Scratch and experimental files live outside the repo or in a scratch folder, never committed as final
- Delete an abandoned approach in the same change that commits to the working one
- No secrets, tokens or credentials in code or config files; use environment variables and a `.env.example`
- Generated files (bin, obj, local CSV output) are ignored via `.gitignore`
- Keep the development journal entry for your work up to date (what, how, which AI, result)

## Definition of done

- Tests for the new behavior exist and are green (test first only where the policy above says so)
- Whole solution builds with 0 warnings, all tests pass
- No forbidden syntax from the table above
- Names follow the table, comments are keyword style
- README of the touched project is up to date

## Projekt: EnergySim

Inhalte nicht hier kopieren, nur verlinken. Einstieg: `README.md`, Doku-Vault `docs/` (in Obsidian als Vault öffnen).

### Aufbau
- Verteiltes System aus getrennten Prozessen, auf einem Rechner oder als Docker-Container: Master (`Time.Cli`), Wetter (`WeatherService`), 4 Geräte-Simulatoren (PV, Batterie, Ladestation, Wind) über `EnergySim.Cli`, Diagnose-Client (`Diagnostic`), Gateway (`Gateway`, Modbus -> RabbitMQ), Viewer (`Viewer`, RabbitMQ -> Konsole)
- Projekte: `Core` (Vertrag, Uhr, Loop, CSV, Register-Map), `Weather`, `WeatherService`, `Modbus` (Uhr-Transport + Geräte-Server), `Time` (Autorität), `Time.Cli` (Master-Exe), `Terminal` (die eine UI), `Pv`, `Battery`, `Charger`, `Wind`, `Cli` (Komposition), `Diagnostic`, `Messaging` (Nachricht, Routing, RabbitMQ), `Gateway`, `Viewer`
- Tests: `tests/EnergySim.UnitTests` (Unit), `tests/EnergySim.IntegrationTests` (echter Server + Client, Label `Category=Integration`), Anleitung `tests/README.md`
- Container: `compose.yaml`, Dockerfiles in `src/EnergySim.{Time.Cli,WeatherService,Cli}/`, Anleitung `docs/user-documentation/docker.md`
- Projektgraph und Ports in `README.md`, keine Zyklen. Jedes Projekt hat ein eigenes `README.md` unter `src/<Projekt>/`

### Regeln
- Gerätprojekte (`Pv`, `Battery`, `Charger`, `Wind`) referenzieren nur Core: Modell + Adapter, kein UI, kein I/O
- Neues Gerät: neues Projekt, von `SimulatedDeviceBase` erben, eine Zeile in `DeviceFactory`
- Logik testbar ohne Konsole, Netzwerk, Datenbank: reine Klassen (z.B. `TelemetryMapper`, `TelemetryFormat`), die Runner rufen sie nur auf
- Verträge in `docs/contracts` (`modbus-registers.md`, `time.md`, `weather.md`, `messaging.md`); Code und Vertrag zusammen ändern
- Gemeinsame Tasten O F L T R stehen in `CommonCommands`, Uhr-Tasten P V + - Q gehören dem Cli/UI, Gerätetasten dürfen sie nicht belegen. Der Master hat zusätzlich S (Single Step) und R (Neustart), das `R` der Geräte ist Fehlerreset
- Virtuelle Zeit kommt immer aus `IClockSource`, nie selbst aufsummieren
- Console nur in `EnergySim.Terminal` (Spectre, `PlainConsoleWriter`), `CliRunner` (Startfehler auf stderr), `Diagnostic`, `Gateway`, `Viewer`
- Alles Warten/I/O async mit `CancellationToken`, EasyModbus (synchron) nur hinter der Grenze in `EnergySim.Modbus`
- Beenden: Ctrl+C, `quit` und SIGTERM (`docker stop`) beenden sauber mit Exit-Code 0
- Keine Geheimnisse im Repo: Zugangsdaten für RabbitMQ nur in `.env` (ignoriert), `.env.example` mit Platzhaltern ist versioniert; sonst Kommandozeile oder `appsettings.json`
- `legacy/` ist Referenz, nicht ändern
- Zu jedem Ticket: `docs/requirements.md` (Status), `docs/todo.md`, betroffene READMEs und Kontrakte, Eintrag in `docs/development-journal/` nachziehen

### Befehle (Repo-Wurzel)
```bash
dotnet build EnergySim.sln                                  # 0 Warnungen
dotnet test EnergySim.sln                                   # Unit + Integration
dotnet test tests/EnergySim.UnitTests                       # nur Unit
dotnet test tests/EnergySim.IntegrationTests                # nur Integration
dotnet test tests/EnergySim.UnitTests --list-tests          # Tests gefunden? 0 ist kein Nachweis
dotnet run --project src/EnergySim.Time.Cli                 # Master
dotnet run --project src/EnergySim.WeatherService           # Wetter
dotnet run --project src/EnergySim.Cli -- --device pv --clock shared   # Gerät (pv|battery|charger|wind)
dotnet run --project src/EnergySim.Diagnostic               # Diagnose-Client
cp .env.example .env && docker compose up -d --wait         # RabbitMQ + Container (master, weather, wind-1)
docker compose stop                                         # sauber beenden
scripts/testrun_task2.sh | scripts/testrun_task3.sh         # Gesamtlauf in eigenen Fenstern
```
Startreihenfolge und Optionen: `README.md`, `docs/user-documentation/`.

### Git im Team
Grundlage ist der Leitfaden "Git-Workflow im Team: Schritt-für-Schritt-Anleitung" (Moodle). Bei Abweichung gilt der Leitfaden, diese Liste ist die Kurzfassung für Agenten und Team.
- Nie direkt auf `main` committen. Ein Branch pro Aufgabe (Ticket): `lb04/<kurzname>` bzw. `<person>/<kurzname>`, angelegt mit `git switch -c` vom aktuellen `main` (oder vom Branch, auf dem die Aufgabe aufbaut, dann im Ticket vermerken)
- Kleine Commits, Nachricht = was und warum (`LB04 WPn: ...`). Nur Dateien der eigenen Aufgabe stagen (Pfade nennen, kein `git add -A` blind), nie `.env`, `bin/`, `obj/`, IDE-Dateien
- Pull Request / Merge Request in `main` mit: was geändert, wie geprüft (Build, Tests, manueller Lauf), offene Punkte. Merge erst nach Prüfung: Build 0 Warnungen, alle Tests grün, Diff gelesen, Funde des Review-Agenten bewertet
- Vor einem Merge: `git fetch`, `main` in den eigenen Branch mergen, Konflikte lokal lösen (beide Seiten lesen, nichts blind "ours/theirs"), erneut bauen und testen
- Aufgabe wechseln: erst `git status` lesen. Änderungen committen oder mit `git stash push -m "<name>"` sichern, nie verwerfen
- Nie fremde oder noch nicht committete Änderungen anderer überschreiben oder verwerfen: kein `git push --force` auf geteilte Branches, kein `git reset --hard`, `git checkout -- <datei>`, `git clean`, `git stash drop` ohne ausdrückliche Rückfrage. Persönliche Journale (`docs/development-journal/<name>.md`) fasst nur die jeweilige Person an
- Agenten committen oder pushen nur auf ausdrückliche Anweisung. Ein Agent-Commit nennt den Agenten in der letzten Zeile (`Co-Authored-By`)
- In LB04 wird jedes Ticket vor dem Commit vom Team geprüft, gepusht wird gesammelt am Ende. Abgabe: annotierter Tag `lb04-2026` auf dem abzugebenden Stand, vollständigen Commit-Hash notieren, Commit und Tag pushen

### Doku-Karte
| Was | Wo |
|---|---|
| Anforderungen, Akzeptanz, Status | `docs/requirements.md` |
| Aufgabenliste, Owner, offene Entscheidungen | `docs/todo.md` |
| Architektur, Entscheidungen, Tests, Konsolidierung | `docs/technical-documentation/` |
| Bedienung, Docker, Fehlersuche | `docs/user-documentation/` |
| Schnittstellen | `docs/contracts/` |
| Nachweise (Läufe, Logs) | `docs/evidence/` |
| Journale | `docs/development-journal/` |

## Skills

One folder per skill under `.agents/skills/<name>/SKILL.md`, agent neutral (Claude, Codex, Copilot). If your agent does not load it by itself: "read `.agents/skills/<name>/SKILL.md` and follow it".

| Skill | Owner | Purpose |
|---|---|---|
| `save-state` | Jakob | handover snapshot `docs/project-state.md` |
| `update-proof-run-scripts` | Jonas | keep proof-run scripts (bash, PowerShell) in sync with the system |
| `sync-agent-config` | Jakob | keep `.agents`, `.claude` and `AGENTS.md` in sync |
| `i-have-adhd` | Jakob | output style for a reader with ADHD (action first, numbered steps, one next action), copy of the external MIT skill i-have-adhd 0.4.1, only on explicit request |
| `check-srs-vs-code` | open | report which `docs/requirements.md` statuses have no proof in code, tests or evidence (report only) |
| `update-development-journal` | Leon | write the next journal entry from the git changes, personal or team journal |
