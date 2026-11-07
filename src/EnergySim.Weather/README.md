# EnergySim.Weather

Wettermodell + Clients. Kein I/O außer Datei laden und HTTP-Client.

## Zweck
- `WeatherModel`: reine Funktion `(Standort, UTC-Zeit) -> WeatherDto`, kein Zufall, kein Zustand
- `LocalWeatherClient`: Modell im selben Prozess (nur im Wetter-Prozess und in Tests)
- `HttpWeatherClient`: fragt den Wetter-Prozess (`EnergySim.WeatherService`)
- `WeatherApi`: Route, Query-Namen, Zeitformat (gemeinsam für Client und Server)

## Verantwortlich
Jakob

## Wetterdaten
| | |
|---|---|
| Quelle | Open-Meteo Historical Weather API |
| Lizenz | CC BY 4.0, Quellenangabe: Open-Meteo.com |
| Zeitraum | 2025-01-01 bis 2025-12-31, stündlich (8760 Werte) |
| Zeitzone | UTC (`timezone: GMT`) |
| Standorte | `salzburg`, `wien`, `innsbruck` (Dateien `data/<id>-2025.json`) |
| Felder | `temperature_2m` °C, `cloud_cover` %, `shortwave_radiation` W/m², `precipitation` mm, `wind_speed_10m` m/s, `wind_direction_10m` ° |

- Anfrage (rekonstruiert aus dem Dateiheader): `https://archive-api.open-meteo.com/v1/archive?latitude=<lat>&longitude=<lon>&start_date=2025-01-01&end_date=2025-12-31&hourly=temperature_2m,cloud_cover,shortwave_radiation,precipitation,wind_speed_10m,wind_direction_10m&wind_speed_unit=ms&timezone=GMT`
- Zur Laufzeit kein Internet nötig, Dateien liegen im Repo

## Grundidee
- Ein Referenzjahr pro Standort. Abfrage über (Monat, Tag, Stunde), das Jahr der Abfrage zählt nicht -> jede Startzeit ist gültig
- Linear zwischen den Stunden: Temperatur, Bewölkung, Strahlung, Windgeschwindigkeit. Windrichtung auf dem kürzesten Bogen. Niederschlag = Stundenwert
- Sonnenhöhe und Azimut: berechnet (NOAA-Näherung) aus Lat/Lon + Zeit
- Reproduzierbar: kein `Random`. `HttpWeatherClient` rundet die Zeit auf die volle Minute ab, so fragen alle Geräte dieselbe Minute
- Toleranz: Abfragezeit wird auf die volle Minute abgerundet (`HttpWeatherClient.floorToMinute`, UTC, Sekunden weg). Grund: zwei Simulatoren mit leicht versetztem Tick bekommen identische Werte. Wetter ändert sich pro Minute kaum, Fehler unter 1 Minute

## Verwendung
```csharp
IReadOnlyDictionary<string, WeatherDataSet> dataSets = await WeatherDataLoader.loadDirectoryAsync(directory, ct);
IWeatherClient local = new LocalWeatherClient(new WeatherModel(dataSets));
IWeatherClient remote = new HttpWeatherClient(new HttpClient(), "http://localhost:5200");
```
Geräte per Kommandozeile: `--weather-url http://localhost:5200` (Standard, Wetter-Prozess ist Pflicht für PV und Wind)

## Schnittstelle
- Bietet: `IWeatherClient` (aus Core), `WeatherApi`
- Braucht: Core. Wetter-Prozess nur für `HttpWeatherClient`
- Vertrag: `docs/contracts/weather.md`

## Fehlerverhalten `HttpWeatherClient`
- Prozess weg, Timeout (2 s) oder ungültige Antwort: letzter gültiger Wert + `IsStale`, Meldung in `ErrorMessage`
- Nie ein gültiger Wert: `Weather = null`, Gerät entscheidet (PV: Leistung 0)
- Kein stilles Weitermachen: die Schleife zeigt `stale` im Dashboard

## Tests
`tests/EnergySim.Tests/Weather/`: Modell, Loader, Sonnenstand, `LocalWeatherClient`, `WeatherReproducibilityTests`

## Einschränkungen
- Nur 3 Standorte, nur Referenzjahr 2025
- 29.2. wird auf 28.2. abgebildet
- Jedes Jahr zeigt dasselbe Wetter
