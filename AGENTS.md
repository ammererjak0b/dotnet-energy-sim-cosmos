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

- Failing test existed first, now green
- Whole solution builds with 0 warnings, all tests pass
- No forbidden syntax from the table above
- Names follow the table, comments are keyword style
- README of the touched project is up to date

## Projekt: EnergySim

- 4 Simulatoren (PV, Batterie, Ladestation, Wind) in einer Konsolenanwendung: `src/EnergySim.Cli`
- Geteilt: `EnergySim.Core` (Vertrag, Uhr, Loop, CSV), `EnergySim.Weather`, `EnergySim.Terminal` (die eine UI)
- Gerätprojekte (`Pv`, `Battery`, `Charger`, `Wind`) referenzieren nur Core: Modell + Adapter, kein UI, kein I/O
- Neues Gerät: neues Projekt, von `SimulatedDeviceBase` erben, eine Zeile in `DeviceFactory`
- Gemeinsame Tasten O F L T R stehen in `CommonCommands`, Uhr-Tasten P V + - Q gehören dem Cli/UI, Gerätetasten dürfen sie nicht belegen
- Virtuelle Zeit kommt immer aus `IClockSource`, nie selbst aufsummieren
- Console nur in `EnergySim.Terminal` (Spectre, `PlainConsoleWriter`) und `CliRunner` (Startfehler auf stderr)
- Alles Warten/I/O async mit `CancellationToken`
- `legacy/` ist Referenz, nicht ändern
