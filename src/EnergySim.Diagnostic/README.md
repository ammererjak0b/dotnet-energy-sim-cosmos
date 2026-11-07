# EnergySim.Diagnostic

Diagnose-Client für die Geräte-Modbus-Server (PV, Batterie, Ladestation, Wind). Liest Header und
Gerätewerte, schreibt On/Off, Limit und Fehlerreset. Verantwortlich: Jakob.

## Konfiguration

| Argument | Standard / Bedeutung |
|---|---|
| `--Host` | überschreibt den Host, Ziel `custom` |
| `--Port` | überschreibt den Port, 1024 bis 65535 |
| `--UnitId` | 1; Bereich 0 bis 255 |

`appsettings.json` enthält `UnitId` und die Zielliste `Targets` (`pv-1` 1511, `battery-1` 1512,
`charger-1` 1513, `wind-1` 1514, alle `127.0.0.1`). Ohne Argumente ist `pv-1` aktiv.
Falsche Argumente: Fehlertext, Usage, Exit-Code 2. Keine Geheimnisse.

## Bedienung

```powershell
dotnet run --project src/EnergySim.Diagnostic
dotnet run --project src/EnergySim.Diagnostic -- --Host 127.0.0.1 --Port 1512
```

| Befehl | Wirkung |
|---|---|
| `header` | 17 Header-Register in einer Anfrage, roh und dekodiert |
| `device` | Gerätewerte laut `DeviceProfiles`: roh, skaliert, Einheit |
| `write on` / `write off` | Holding 0 |
| `write limit <0..100>` | Holding 1 in Promille; Bereich wird vor dem Senden geprüft |
| `write reset` | Holding 2, Fehlerreset |
| `watch` | Header jede Sekunde bis zu einer Taste (bei umgeleiteter Eingabe bis Ctrl+C) |
| `target [Name\|Nummer]` | Ziel wechseln |
| `quit` | beenden, Ctrl+C ebenso, Exit-Code 0 |

Nach jedem Schreiben wird der Header neu gelesen und die Wirkung gezeigt. Warn-Bit 2
(InvalidWrite) wird markiert.

## Schnittstellen

`EnergySim.Modbus`: `RegisterMap`, `RegisterCodec`, `DeviceProfiles`; `ModbusRegisterClient`
(synchrone Bibliotheksgrenze), `DiagnosticDevice` prüft das CancellationToken um jeden Aufruf. Vertrag:
`docs/contracts/modbus-registers.md`.

## Einschränkungen

- Verbindung weg oder Gerät aus: lesbare Meldung, der nächste Befehl verbindet neu.
- Unbekannter Gerätetyp oder Map-Version: Meldung und Abbruch mit Exit-Code 1.
- Kein Schreibzugriff auf Gerätewerte, nur die drei Holding-Register. Keine automatischen Tests.
