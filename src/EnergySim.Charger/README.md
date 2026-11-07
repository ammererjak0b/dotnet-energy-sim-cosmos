# EnergySim.Charger

Ladestation mit zwei Ports für die gemeinsame .NET-10-Solution. Verantwortlich: Leon Angerer.

## Zweck

- Verbraucher: lädt E-Autos an Port 1 und Port 2
- Autos kommen und gehen nach Zeitplan (UTC, täglich) oder per Taste
- Gesamtleistung begrenzt, Aufteilung fair, über `TaperStartSocPercent` sinkt die Leistung linear bis 100 %
- Kein Wetter nötig (`NeedsWeather = false`)

## Konfiguration

Abschnitt `Charger` in `src/EnergySim.Cli/appsettings.json` oder als Argument (`--Charger:TotalMaxPowerKw 11`).

| Key | Standard | Bedeutung |
|---|---:|---|
| `Charger:Port1MaxPowerKw` | 11 kW | Maximalleistung Port 1 |
| `Charger:Port2MaxPowerKw` | 11 kW | Maximalleistung Port 2 |
| `Charger:TotalMaxPowerKw` | 22 kW | Summe beider Ports |
| `Charger:TaperStartSocPercent` | 80 % | ab hier sinkt die Leistung |
| `Charger:Schedule` | 2 Demo-Autos | Liste: Name, CapacityKwh, StartSocPercent, MaxPowerKw, Port, Arrive/Depart (Hour/Minute UTC) |

Modbus (gemeinsam, siehe `src/EnergySim.Modbus/README.md`):

| Key | Standard | Bedeutung |
|---|---|---|
| `Modbus:Host` | `0.0.0.0` | Bind-Adresse |
| `Modbus:Port` | 1513 (Charger) | TCP-Port, mindestens 1502 |
| `Modbus:UnitId` | 1 | Unit ID, Client muss gleich sein |
| `Device:Id` | - | Geräte-ID, nicht leer |

## Starten

```powershell
dotnet run --project src/EnergySim.Cli -- --device charger
```

Tasten: `O` ein, `F` aus, `L` Limit, `T` Störung, `R` Fehlerreset, `C` Auto anstecken, `D` Auto abstecken, `N` Port-Störung, `E` Port-Störung löschen (C/D/N/E mit Portnummer 1 oder 2). `P` Pause, `Q` beenden.

## Schnittstellen

- `ChargerDevice` erbt von `SimulatedDeviceBase`, Typ `DeviceType.Charger = 3`
- Header, `SpecificValues`, `DisplayRows`, CSV-Felder, Befehle über den gemeinsamen Gerätevertrag
- Modbus-Server und Registerplan: `docs/contracts/modbus-registers.md`

## Vorzeichen

- Erzeugung positiv, Verbrauch negativ
- Laden = Verbraucher: `ActivePowerW` und `Port1Power`/`Port2Power` sind negativ
- Anzeige ("Charging power", Portzeilen) zeigt positive kW, nur zur Lesbarkeit

## Registerplan

Byte order: big-endian pro Register. Word order: High-Word zuerst (ABCD). Adressen 0-basiert (Client-Adresse). Alle Zähler und Leistungen s32/u32 über 2 Register.

### Header (Input, FC04) und Schreibregister (Holding, FC03/FC06)

Vollständig in `docs/contracts/modbus-registers.md`. Charger-Verhalten:

| Typ | Adresse / Ref | Datentyp (Anzahl) | Skalierung, Einheit | Zugriff | Bereich | Charger-Verhalten |
|---|---|---|---|---|---|---|
| Input | 0-16 / 30001-30017 | u16/s32/u32/u64 (17) | 1 | R | siehe Contract | DeviceType = 3 |
| Input | 5 / 30006 | s32 (2) | 1 W | R | -22000..0 (Standard) | Summe beider Ports, negativ, 0 bei Aus/Fault |
| Input | 7 / 30008 | u32 (2) | 1 Wh | R | 0.. | geladene Energie, Reset bei Neustart |
| Input | 3 / 30004 | u16 (1) | 1 | R | 0..3 | 0 ok, 1 Charger, 2 Port 1, 3 Port 2 (Charger vor Port 1 vor Port 2) |
| Holding | 0 / 40001 | u16 (1) | 1 | R/W | 0..1 | Ein/Aus, Aus = 0 W |
| Holding | 1 / 40002 | u16 (1) | 0.1 % | R/W | 0..1000 | Limit auf `TotalMaxPowerKw` |
| Holding | 2 / 40003 | u16 (1) | 1 | R/W | 0..1 | 1 = Fehler löschen (Charger und Ports) |

Ungültiger Schreibwert: alter Wert zurück, Warn-Flag b2.

### Device-Block (Input, FC04, nur lesen)

Wert i bei Adresse `100 + 2 * i`, Referenz `30101 + 2 * i`. Typ s32, 2 Register, Raw = Wert * Skalierung. Byte order big-endian, Word order High-Word zuerst.

| i | Name | Adresse / Ref | Typ (Anzahl) | Skalierung, Einheit | Zugriff | Bereich (Raw) | Bedeutung, Fehlerverhalten |
|---|---|---|---|---|---|---|---|
| 0 | Port1Power | 100 / 30101 | s32 (2) | x1, W | R | -11000..0 | Ladeleistung Port 1, negativ. 0 bei frei, voll, Port-Fault, Charger Aus/Fault |
| 1 | Port2Power | 102 / 30103 | s32 (2) | x1, W | R | -11000..0 | wie Port 1 |
| 2 | Port1VehicleSoc | 104 / 30105 | s32 (2) | x10, % | R | 0..1000 | SoC des Autos an Port 1. 0 wenn kein Auto steckt. Bleibt bei Fault/Aus erhalten |
| 3 | Port2VehicleSoc | 106 / 30107 | s32 (2) | x10, % | R | 0..1000 | wie Port 1 |

Bereich Leistung = `-PortNMaxPowerKw * 1000` bei Standardwerten. Alle Device-Block-Register sind nicht schreibbar. Port-Status (frei, lädt, Fault, aus) ist im Modell vorhanden (`getPortStatus`), aber nicht im Profil: nur Anzeige und CSV.

## Tests

```powershell
dotnet build EnergySim.sln
dotnet test tests/EnergySim.Tests/EnergySim.Tests.csproj --filter FullyQualifiedName~EnergySim.Tests.Charger
```

Tests liegen in `tests/EnergySim.Tests/Charger/` (Physik, Zeitplan, Befehle, Anzeige). Kein Netzwerk, feste Zeitschritte.

## Bekannte Einschränkungen

- Ladeleistung ist pro Schritt konstant, kein Ladeverlust, keine Temperaturmodell
- Auto bei 100 % zeigt sofort 0 W, bleibt aber angesteckt bis Abfahrtszeit
- Zeitplan gilt täglich in UTC, keine Wochentage
- Port-Status und Fault-Quelle (Port 1 oder 2) nur über Fault-Code, nicht im Device-Block
- Limit wirkt nur auf die Gesamtleistung, nicht pro Port
