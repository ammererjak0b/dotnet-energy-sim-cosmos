# EnergySim.Pv

PV-Anlage: Wetter rein, Leistung und Energie raus. Reines Modell (`PvPlant`) + Adapter (`PvDevice`), kein I/O, keine Uhr.

## Verantwortlich
Jakob Ammerer

## Konfiguration
| Key | Standard | Bedeutung |
|---|---|---|
| `Pv:PeakPowerKw` | 5.0 | kWp bei 1000 W/m2 und 25 °C, > 0 |
| `Pv:PerformanceRatio` | 0.8 | Verluste als ein Faktor, (0, 1] |
| `location` (CLI `--location`) | `salzburg` | `salzburg`, `wien`, `innsbruck` |
| `weather-url` (CLI `--weather-url`) | `http://localhost:5200` | Wetter-Prozess, Pflicht für PV |

Ungültige Werte -> `ArgumentOutOfRangeException` mit Property-Name beim Start.

## Verwendung
```
dotnet run --project src/EnergySim.Cli -- --device pv --location salzburg --weather-url http://localhost:5200
```
Tasten: `O` ein, `F` aus, `L` Limit, `T` Störung, `R` Fehlerreset.

## Schnittstellen
- Bietet: `PvDevice : ISimulatedDevice` (Header, DisplayRows, `SpecificValues`, Befehle)
- Braucht: Core, Wetter über `IWeatherClient` (`NeedsWeather = true`), `docs/contracts/weather.md`
- Modbus-Server liegt in `EnergySim.Modbus`, liest Header + `SpecificValues` pro Frame

## Modell
- Leistung = `PeakPowerKw * Einstrahlung/1000 * Temperaturfaktor * PerformanceRatio`
- Modultemperatur = Außentemperatur + 0,025 K pro W/m2
- Temperaturfaktor = -0,4 % pro K über 25 °C
- Limit kappt Leistung, `Limited` wenn gekappt
- Kein Wetter -> Leistung 0, Status `Standby`, Wetterwerte 0

## Gerätekopf
- `ActivePowerW`: Erzeugung positiv, 0 bei Aus/Störung/kein Wetter
- `EnergyWh`: kumuliert, bei `reset` und Neustart 0
- `FaultCode`: 0 = ok, 1 = Inverter-Störung bis Reset
- Ein/Aus und Limit über gemeinsame Befehle (`O`, `F`, `L`)

## Tests
`tests/EnergySim.Tests/Pv/`: Optionen, Modell, Adapter

## Einschränkungen
- Ein Strang, keine Ausrichtung/Neigung, keine Verschattung
- `DailyEnergy` zählt ab 00:00 UTC (nicht lokale Mitternacht)
- Modultemperatur ist 0 solange kein Wetter da ist
- Einstrahlung = `shortwave_radiation` (global horizontal)
- CLI-Anbindung der Modbus-Keys steht aus (CLI aktuell nur `--id`)

# Registerplan

Quelle der Wahrheit: `docs/contracts/modbus-registers.md`, Code `EnergySim.Modbus.DeviceProfiles`.
Adressen 0-basiert (Protokolladresse), Referenz = klassische Modicon-Nummer.
Byte-Reihenfolge Big-Endian pro Register, Wort-Reihenfolge High-Word zuerst (ABCD).

## Modbus-Konfiguration
| Key | Standard | Bedeutung |
|---|---|---|
| `Modbus:Host` | `0.0.0.0` | Bind-Adresse |
| `Modbus:Port` | 1511 (PV) | ab 1502 |
| `Modbus:UnitId` | 1 | 1..247, anderer Wert -> keine Antwort |
| `Device:Id` | leer | Geräte-ID, z.B. `pv-1` |

## Gemeinsamer Kopf (Input Register, FC04, nur lesen)
| Typ | Adresse | Referenz | Datentyp | Anzahl | Skalierung | Einheit | Bereich | Bedeutung |
|---|---|---|---|---|---|---|---|---|
| IR | 0 | 30001 | u16 | 1 | 1 | - | 1 | DeviceType, PV = 1 |
| IR | 1 | 30002 | u16 | 1 | 1 | - | 1.. | Register-Map-Version |
| IR | 2 | 30003 | u16 | 1 | 1 | - | 0..4 | State: 0 Off, 1 Standby, 2 Running, 3 Limited, 4 Fault |
| IR | 3 | 30004 | u16 | 1 | 1 | - | 0..65535 | Fehlercode, 0 = ok, PV: 1 = Inverter-Störung |
| IR | 4 | 30005 | u16 | 1 | Bits | - | b0..b2 | Warn: b0 NoMasterSync, b1 WeatherStale, b2 InvalidWrite |
| IR | 5 | 30006 | s32 | 2 | 1 | W | 0..Peak | Wirkleistung, Erzeugung positiv, 0 bei Aus/Störung |
| IR | 7 | 30008 | u32 | 2 | 1 | Wh | 0.. | Energie kumuliert, 0 nach Neustart |
| IR | 9 | 30010 | u64 | 4 | 1 | ms | Unix ms UTC | Virtuelle Zeit, eingefroren bei Pause |
| IR | 13 | 30014 | u32 | 2 | 1 | - | Überlauf | Sequenz, +1 pro neuem Zustand |
| IR | 15 | 30016 | u16 | 1 | 1 | - | 0..65535 | Generation, +1 pro Neustart |
| IR | 16 | 30017 | u16 | 1 | 1 | - | 0..3 | Clock: 0 Stopped, 1 Running, 2 Paused, 3 Unsynced |

Alle 17 Register mit einer Anfrage lesbar.

## Geräteblock PV (Input Register, FC04, nur lesen)
Wert i liegt bei `100 + 2 * i`, s32, 2 Register, Rohwert = Wert * Skalierung.

| Typ | Adresse | Referenz | Datentyp | Anzahl | Wort-Order | Skalierung | Einheit | Lesen/Schreiben | Bereich | Bedeutung und Fehlerverhalten |
|---|---|---|---|---|---|---|---|---|---|---|
| IR | 100 | 30101 | s32 | 2 | ABCD | x1 | W/m2 | R | 0..1500 | Einstrahlung (global horizontal). Kein Wetter -> 0 |
| IR | 102 | 30103 | s32 | 2 | ABCD | x10 | °C | R | -400..900 (-40,0..90,0) | Modultemperatur = Außen + 0,025 K pro W/m2. Kein Wetter -> 0 |
| IR | 104 | 30105 | s32 | 2 | ABCD | x10 | ° | R | -900..900 (-90,0..90,0) | Sonnenhöhe, negativ in der Nacht. Kein Wetter -> 0 |
| IR | 106 | 30107 | s32 | 2 | ABCD | x1 | Wh | R | 0.. | Energie seit 00:00 UTC (virtueller Tag), Reset bei Tageswechsel und Neustart |

Kein Wetter (Prozess nie erreichbar): Werte 0. Wetter veraltet: letzter gültiger Wert, Warn-Bit b1.

## Schreibbare Register (Holding, FC03 lesen, FC06 schreiben)
| Typ | Adresse | Referenz | Datentyp | Anzahl | Skalierung | Einheit | Bereich | Bedeutung und Fehlerverhalten |
|---|---|---|---|---|---|---|---|---|
| HR | 0 | 40001 | u16 | 1 | 1 | - | 0..1 | Ein/Aus. Ungültig -> alter Wert, Warn-Bit b2 |
| HR | 1 | 40002 | u16 | 1 | 0,1 | % | 0..1000 | Leistungslimit in Promille der Nennleistung. Ungültig -> alter Wert, Warn-Bit b2 |
| HR | 2 | 40003 | u16 | 1 | 1 | - | 0..1 | Fehlerreset, 1 löscht Störung, Server setzt auf 0 zurück. Ohne Störung ignoriert |

Port-Standard PV: Modbus 1511, Clock-Endpunkt 1601, Wetter 5200.

AI-USAGE: Claude Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Registerplan, Tabellen)
