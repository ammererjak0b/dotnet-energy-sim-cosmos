# Nachrichtenvertrag LB03

## Topologie
| Teil | Wert |
|---|---|
| Protokoll | AMQP 0-9-1 |
| Exchange | `energysim.telemetry`, Typ `topic`, durable |
| Routing Key | `telemetry.<deviceType>.<deviceId>`, z.B. `telemetry.pv.pv-1` |
| deviceType | `pv`, `battery`, `charger`, `wind` |
| Viewer-Queue | vom Broker benannt, exclusive, auto-delete |
| Binding | `telemetry.#` (alle), `telemetry.pv.*` (nur PV) |
| Body | JSON, UTF-8, `content-type: application/json`, non-persistent |

## Beispiel
```json
{
  "schemaVersion": 1,
  "deviceId": "pv-1",
  "deviceType": "Pv",
  "simTimeUtc": "2026-06-21T06:30:00Z",
  "polledAtUtc": "2026-11-12T10:15:03.412Z",
  "sequence": 1842,
  "generation": 0,
  "clockState": "Running",
  "state": "Running",
  "faultCode": 0,
  "warnFlags": [],
  "quality": "Good",
  "measurements": [
    { "name": "ActivePower", "value": 3120, "unit": "W" },
    { "name": "Energy", "value": 15400, "unit": "Wh" },
    { "name": "Irradiance", "value": 640, "unit": "W/m2" }
  ],
  "error": ""
}
```

## Felder
| Feld | Bedeutung |
|---|---|
| `deviceId` | Id aus der Gateway-Konfiguration, eindeutig |
| `simTimeUtc` | virtuelle Zeit des Gerätestands (Register), UTC, `null` bei `Bad` |
| `polledAtUtc` | echte Uhrzeit der Abfrage, nie mit `simTimeUtc` mischen |
| `sequence` | Zähler neuer Gerätestände, +1 je Stand, steht bei Pause, wrap bei u32 |
| `generation` | Register `Generation`, ändert sich bei Neustart/Reset des Geräts |
| `state`, `faultCode` | Gerätezustand aus Header, Fault ist Messdaten, keine Qualität |
| `warnFlags` | `NoMasterSync`, `WeatherStale`, `InvalidWrite` |
| `measurements` | Header (`ActivePower`, `Energy`) + Gerätewerte aus `DeviceProfiles`, Einheit je Wert |
| `error` | Grund bei `Bad`, sonst leer |

## Quality
| Wert | Wann |
|---|---|
| `Good` | Lesen ok, keine Warnung, Clock nicht `Unsynced` |
| `Uncertain` | `NoMasterSync` oder `WeatherStale` oder Clock `Unsynced` |
| `Bad` | Modbus- oder Dekodierfehler, `measurements` leer |

## Verhalten
- Pause: `sequence` ändert sich nicht. Gateway publiziert einmal beim Wechsel von `clockState`/`state`/Warnungen, danach nicht erneut
- Gerät weg: einmal `Bad` je Ausfall, danach erst wieder bei Erfolg
- Broker weg: Stände verworfen und gezählt, nach Reconnect der aktuelle Stand

AI-USAGE: Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Nachrichtenvertrag LB03)
