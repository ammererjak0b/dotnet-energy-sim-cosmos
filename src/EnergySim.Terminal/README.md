# EnergySim.Terminal

Die eine Konsolen-UI für alle Prozesse (Geräte-Cli und Master).

## Zweck
- Dashboard (Spectre.Console): Status, Werte, Uhr, letzte Logzeilen, Befehlsliste
- Tastenquelle: nicht blockierend (`IKeySource`, `ConsoleKeySource`)
- Log-Puffer: letzte N Zeilen, thread-sicher (`LogBuffer`, `DashboardLoggerProvider`), nur der UI-Task schreibt in die Konsole
- Headless-Ausgabe: reine Zeilen `HH:mm:ss LVL message` (`PlainConsoleWriter`), Ende per `quit` auf stdin (`StdinQuitWatcher`)

## Verantwortlich
Leon Angerer

## Konfiguration
- Keine eigene. Der Host liefert `IDashboardSource` (Zustand, Befehle, Shutdown)
- Interaktiv = Ein- und Ausgabe nicht umgeleitet (`TerminalMode.isInteractive`), sonst Headless
- Zeitanzeige: Wien-Zeit plus UTC (`VirtualTimeFormat`)

## Verwendung
- Cli und Time.Cli bauen `DeviceDashboard` mit ihrer `IDashboardSource`
- Taste `Q` beendet, weitere Tasten kommen aus der Befehlsliste der Quelle
- Logging des Hosts an `DashboardLoggerProvider` hängen, nie direkt `Console.Write`

## Schnittstelle
- Bietet: `IDashboardSource`, `IKeySource`, `DeviceDashboard`, `LogBuffer`, `PlainConsoleWriter`, `StdinQuitWatcher`, `TerminalMode`
- Braucht: `Core`, Spectre.Console
- Kein Modell, kein Modbus, kein HTTP

## Tests
`tests/EnergySim.Tests/Terminal/`: Dashboard, Blocks, LogBuffer, Logging, Mode, `StdinQuitWatcher`, `VirtualTimeFormat` (Fake-Tastenquelle, kein echtes Terminal)

## Einschränkungen
- Tasten per `Console.KeyAvailable`-Polling, kein Event. Funktioniert nur ohne umgeleitete Eingabe
- Umgeleitete Ein- oder Ausgabe: kein Dashboard, nur Statuszeilen
