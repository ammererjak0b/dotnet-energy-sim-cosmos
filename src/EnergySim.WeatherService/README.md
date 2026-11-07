# EnergySim.WeatherService

Eigener Wetter-Prozess (Exe). Geräte fragen Wetter per HTTP ab statt es selbst zu rechnen.

## Zweck
- Ein Wetter für alle Simulatoren: gleicher Ort + gleiche virtuelle Zeit = gleiche Werte
- Zustandslos, keine eigene Uhr: die virtuelle Zeit kommt mit der Abfrage
- Offline, Daten liegen im Repo (`EnergySim.Weather/data`)

## Verantwortlich
Jakob

## Start
```bash
dotnet run --project src/EnergySim.WeatherService                 # Port 5200
dotnet run --project src/EnergySim.WeatherService -- --port 5201
```
- Startreihenfolge: Master, **Wetter**, Geräte, Diagnose-Client
- Ende: Ctrl+C, Exit-Code 0
- Port belegt: Meldung, Exit-Code 1. Ungültiger Port (< 1024): Exit-Code 2

## Konfiguration
| Option | Bedeutung | Standard |
|---|---|---|
| `--port` | HTTP-Port | 5200 |

## Schnittstelle
`GET /api/weather?location=salzburg&time=2025-07-01T12:00:00Z`

| Code | Bedeutung |
|---|---|
| 200 | JSON `WeatherDto` |
| 400 | `location` fehlt oder `time` nicht UTC `yyyy-MM-ddTHH:mm:ssZ` |
| 404 | Standort unbekannt oder falsche Route |
| 500 | Datenfehler |

- Vertrag: `docs/contracts/weather.md`
- Client: `HttpWeatherClient` (in `EnergySim.Weather`), Geräte starten mit `--weather-url http://localhost:5200`

## Konsole
Eine Zeile pro neue `(Standort, Minute)`: Werte + Anzahl Abfragen. Gleiche Zeile für zwei Geräte = gleiches Wetter.

## Tests
`tests/EnergySim.Tests/Weather/WeatherReproducibilityTests.cs`: gleiche Abfrage, gemischte Reihenfolge, Minuten-Rundung, Ausfall (letzter Wert + stale)

## Einschränkungen
- Referenzjahr 2025, Jahr der Abfrage wird ignoriert
- Abfragen werden nacheinander bearbeitet (reicht für wenige Geräte)
- Kein HTTPS, keine Authentifizierung (nur lokales Netz)
- Console-Ausgabe in `Program.cs`: eigene Exe, nicht Teil von `EnergySim.Terminal`
