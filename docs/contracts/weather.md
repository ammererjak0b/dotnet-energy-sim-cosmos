# Vertrag: Wetterabfrage

Server: `EnergySim.WeatherService` (Port 5200). Client: `HttpWeatherClient`. Typen: `WeatherApi`, `WeatherDto` (Core).

## Request
`GET /api/weather?location=<id>&time=<UTC>`

| Parameter | Format | Beispiel |
|---|---|---|
| `location` | Standort-ID | `salzburg`, `wien`, `innsbruck` |
| `time` | UTC, `yyyy-MM-ddTHH:mm:ssZ` | `2025-07-01T12:00:00Z` |

- Zeit = virtuelle Zeit des Aufrufers. Der Wetter-Prozess hat keine eigene Uhr
- Client rundet auf die volle Minute ab -> gleiche Minute, gleiche Werte, Reihenfolge egal

## Response 200
JSON `WeatherDto`, Zeiten UTC

| Feld | Einheit |
|---|---|
| `LocationId` | - |
| `TimeUtc` | ISO 8601 |
| `SunElevationDeg`, `SunAzimuthDeg` | ° |
| `CloudCoverPct` | % |
| `TemperatureC` | °C |
| `PrecipitationMmPerH` | mm/h |
| `WindSpeedMs`, `WindDirectionDeg` | m/s, ° |
| `GlobalIrradianceWm2` | W/m² |

## Fehler
| Code | Ursache | Body |
|---|---|---|
| 400 | `location` fehlt, `time` fehlt oder nicht UTC | Text |
| 404 | Standort unbekannt, falsche Route | Text |
| 500 | Datenfehler | Text |

## Client-Verhalten
- Fehler, Timeout (2 s), Prozess weg -> letzter gültiger Wert des Standorts + `IsStale = true`
- Noch nie ein Wert -> `Weather = null`, `ErrorMessage` gesetzt
- Ctrl+C des Aufrufers bricht ab, kein Fehlerwert

## Garantien
- Zustandslos, kein Zufall: gleicher Standort + gleiche Minute = gleiche Werte
- Offline
- Referenzjahr 2025, Jahr der Abfrage wird ignoriert
