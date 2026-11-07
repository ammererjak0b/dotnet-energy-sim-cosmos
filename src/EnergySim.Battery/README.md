# EnergySim.Battery

Batteriespeicher für die gemeinsame .NET-10-Solution. Verantwortlich: Jonas Kreibich.

## Zweck

Simuliert einen Speicher mit SoC, Zelltemperatur, Lade-/Entladeleistung und Energiezählern.
Kein Wetter nötig (`NeedsWeather = false`). Startet eingeschaltet im Automodus (Zeitplan).

## Konfiguration

Abschnitt `Battery` (`BatteryOptions`), Standard = LB01-Werte:

| Schlüssel | Standard | Bedeutung |
|---|---:|---|
| `CapacityKWh` | 100 | Kapazität |
| `MaxChargePowerKW` / `MaxDischargePowerKW` | 50 / 50 | Maximalleistung |
| `InitialSocPercent` | 50 | Start-SoC |
| `MinimumSocPercent` / `MaximumSocPercent` | 10 / 90 | SoC-Fenster |
| `ChargeEfficiencyPercent` / `DischargeEfficiencyPercent` | 95 / 95 | Wirkungsgrade |
| `InitialTemperatureC` / `AmbientTemperatureC` | 25 / 22 | Temperatur |
| `WarningTemperatureC` / `FaultTemperatureC` / `ResetTemperatureC` | 40 / 50 / 40 | Warnung, Fehler, Reset nur darunter |
| `RandomSeed` | 42 | Automodus |

Ungültige Werte werden beim Start mit Verweis auf den Abschnitt abgelehnt.

## Bedienung

```powershell
dotnet run --project src/EnergySim.Cli -- --device battery
dotnet run --project src/EnergySim.Cli -- --device battery --Battery:CapacityKWh 200
```

Gemeinsame Tasten: `O` ein, `F` aus, `L` Limit, `T` Störung, `R` Reset. Zusätzlich `A` Automodus, `M` manuelle Leistung (negativ = laden, positiv = entladen).

## Schnittstellen

- Bietet: `ISimulatedDevice` (`BatteryDevice`), Header, `DisplayRows`, `SpecificValues`, CSV-Felder, Befehle.
- Braucht: nur `EnergySim.Core`. Zeit kommt vom gemeinsamen Loop.
- Header: `ActivePowerW` Entladen positiv, Laden negativ. `EnergyWh` = kumulierte Entladeenergie (Erzeugungsseite), 0 nach Neustart. `FaultCode` 0 = ok, 1 = Übertemperatur. Limit und Ein/Aus über die gemeinsamen Befehle.

## Tests

```powershell
dotnet test EnergySim.sln
```

Tests unter `tests/EnergySim.Tests/Battery`. Deterministisch, ohne Netzwerk und Wartezeit.

## Bekannte Einschränkungen

- Vereinfachtes Temperaturmodell, keine Herstellerdaten.
- `EnergyWh` im Kopf zählt nur Entladung. Ladeenergie steht im Gerätebereich (`ChargedEnergy`).
- Fehlerreset nur bei Zelltemperatur <= `ResetTemperatureC`, sonst abgelehnt.

## Registerplan

### Gemeinsamer Kopf und Schreibregister

Gemeinsamer Kopf (Input, FC04, nur lesen, Adresse 0..16) und Holding-Register (FC03/FC06, Adresse 0..2) laut `docs/contracts/modbus-registers.md`. Alle Zahlen big-endian, 32/64 Bit mit High-Word zuerst (ABCD).

| Typ | Adresse / Referenz | Register | Datentyp | Skalierung, Einheit | Zugriff | Bereich | Bedeutung, Fehlerverhalten |
|---|---|---|---|---|---|---|---|
| Input | 0 / 30001 | 1 | u16 | 1 | R | 1..4 | DeviceType |
| Input | 1 / 30002 | 1 | u16 | 1 | R | 1.. | Registerplan-Version |
| Input | 2 / 30003 | 1 | u16 | 1 | R | 0..4 | State (Off, Standby, Running, Limited, Fault) |
| Input | 3 / 30004 | 1 | u16 | 1 | R | 0..65535 | FaultCode, 0 = ok |
| Input | 4 / 30005 | 1 | u16 | Bits | R | b0..b2 | Warn-Flags |
| Input | 5 / 30006 | 2 | s32 | 1 W | R | gerätespezifisch | Wirkleistung, Erzeugung positiv |
| Input | 7 / 30008 | 2 | u32 | 1 Wh | R | 0.. | Energie kumuliert, Reset bei Neustart |
| Input | 9 / 30010 | 4 | u64 | 1 ms | R | Unix-ms UTC | Virtuelle Zeit |
| Input | 13 / 30014 | 2 | u32 | 1 | R | wrap | Sequenz |
| Input | 15 / 30016 | 1 | u16 | 1 | R | 0..65535 | Generation, +1 pro Neustart |
| Input | 16 / 30017 | 1 | u16 | 1 | R | 0..3 | Clock-State |
| Holding | 0 / 40001 | 1 | u16 | 1 | R/W | 0..1 | Ein/Aus. Ungültig: alter Wert zurück, Warn-Flag b2 |
| Holding | 1 / 40002 | 1 | u16 | 0,1 % | R/W | 0..1000 | Leistungslimit (Promille der Nennleistung). Ungültig: alter Wert zurück, Warn-Flag b2 |
| Holding | 2 / 40003 | 1 | u16 | 1 | R/W | 0..1 | Fehlerreset, Server setzt auf 0 zurück. Ohne Fehler ignoriert |

Modbus-Konfiguration:

| Schlüssel | Standard | Bedeutung |
|---|---|---|
| `Modbus:Host` | siehe Contract | Bind-Adresse des Servers |
| `Modbus:Port` | 1512 | TCP-Port |
| `Modbus:UnitId` | siehe Contract | Unit-ID |
| `Device:Id` | `--id` bzw. Gerätename | Geräte-ID |

### Gerätebereich Battery

Input-Register (FC04, nur lesen), Wert i bei Adresse `100 + 2 * i`, je 2 Register, s32, Big-Endian, High-Word zuerst. Rohwert = Wert * Skalierung, gerundet.

| Typ | Adresse / Referenz | Register | Datentyp | Skalierung, Einheit | Zugriff | Bereich | Bedeutung, Fehlerverhalten |
|---|---|---|---|---|---|---|---|
| Input | 100 / 30101 | 2 | s32 | x10, % | R | 0..1000 (0..100 %), Fenster 10..90 % | StateOfCharge. Immer gültig |
| Input | 102 / 30103 | 2 | s32 | x10, degC | R | ca. 200..600 | CellTemperature. Ab 50 degC Störung (FaultCode 1) |
| Input | 104 / 30105 | 2 | s32 | x1, Wh | R | 0.. | ChargedEnergy, kumuliert, 0 nach Neustart |
| Input | 106 / 30107 | 2 | s32 | x1, Wh | R | 0.. | DischargedEnergy, kumuliert, 0 nach Neustart |

Alle vier Werte stammen direkt aus dem Modell. Kein Wert ist abgeleitet oder 0.
