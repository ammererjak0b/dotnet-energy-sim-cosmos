# LB02 Laufnachweis

Lauf: `scripts/proof-run.sh docs/evidence/logs` (nach `dotnet build EnergySim.sln`). Alle Programme eigene Prozesse, headless, Stdin über Fifos.
Gestartet: Master, Wetter (5200), pv-1 (1511/1601), battery-1 (1512/1602), charger-1 (1513/1603), wind-1 (1514/1604), pv-2 (1521/1611, `--id pv-2`, gleicher Ort `salzburg`), drei Diagnostic-Clients (pv-1, battery-1, pv-2).
Master-Port: 1520 statt 1502 (`MASTER_PORT=1520`), weil 1502 von einem anderen Master auf dem Rechner belegt war. Gerätestart mit `--time-port 1520`.
Ablauf und Zeitstempel: [steps.log](logs/steps.log). Master: [master.log](logs/master.log). Wetter: [weather.log](logs/weather.log).

| # | Schritt | Befehl / Beobachtung | Ergebnis |
|---|---|---|---|
| 1 | Alle Programme getrennt gestartet | 8 Prozesse, Master-Tabelle `participants: 5`, alle `OK`, `missed 0` ([master.log](logs/master.log)) | pass |
| 2 | Virtuelle Zeiten gleich | `header` auf pv-1/battery-1/pv-2 bei Tempo 60, Skew siehe unten | pass |
| 3 | Tempo 60 -> 600 -> 60 im Lauf | `speed 600`, `speed 60`; Differenz bleibt klein, kein Auseinanderlaufen ([diag-pv-1.log](logs/diag-pv-1.log), SAMPLE 6-11) | pass |
| 4 | Pause | `pause`; zweimal `header` im Abstand 3 s: Zeit gleich, `clock Paused`, `device` antwortet weiter (SAMPLE 12, 13) | pass |
| 5 | Resume | `resume`; Zeit läuft von der Pausenzeit weiter, kein Sprung (SAMPLE 14, 15) | pass |
| 6 | Restart | `restart 2026-06-22 13:00`; alle drei Geräte `gen 2`, Zeit ab 11:00Z, Master-Skew weiter klein (SAMPLE 16-18) | pass |
| 7 | Gleiches Wetter | pv-1 und pv-2 pausiert: gleiche Zeit `2026-06-22T11:09:33.155Z`, Irradiance 920, ModuleTemperature 51.4, SunElevation 65.6; [weather.log](logs/weather.log): eine Zeile pro Minute, `15 requests` | pass |
| 8 | Diagnostic liest Header + Gerätewerte | `header`, `device` auf pv-1, battery-1 ([diag-battery-1.log](logs/diag-battery-1.log)) | pass |
| 9 | Diagnostic schreibt Limit | `write limit 50`: pv-1 `State Limited`, Leistung 3,29 kW -> 2,5 kW, pv-2 (ohne Limit) bleibt bei 3,29 kW ([pv-1.log](logs/pv-1.log), [pv-2.log](logs/pv-2.log)); `write limit 150` wird vor dem Senden abgelehnt | pass |
| 10 | Kontrolliertes Ende | `quit` an Geräte/Diagnostic/Master, SIGINT an Wetter: alle 10 Prozesse Exit-Code 0 ([steps.log](logs/steps.log), Zeilen `exit`) | pass |

## Skew

Zwei Messwege:

- **Master-Skew-Spalte** (Gerät gegen Master-Timeline, virtuelle ms): 195 Werte aus [master.log](logs/master.log)
- **Header-Register 9..12** (Zeit des letzten Zustands) von zwei Geräten kurz hintereinander gelesen: 19 Samples je Paar, `battery-1 - pv-1` und `pv-2 - pv-1`

| Messung | Tempo 60 | Tempo 600 |
|---|---|---|
| Master-Skew (virtuell) | 0 bis -7 ms | -2 bis -7 ms |
| Header, battery-1 gegen pv-1, max Betrag (11 / 5 Samples) | 9,8 s virtuell = 163 ms real | 23,2 s virtuell = 39 ms real |
| Header, pv-2 gegen pv-1, max Betrag | 11,5 s virtuell = 192 ms real | 5,0 s virtuell = 8 ms real |
| Pause (SAMPLE 12, 13, 19) | 0 ms | - |

- Header-Zeit = Zeit des letzten berechneten Zustands. Das Gerät rechnet alle 200 ms real einen Zustand => Header-Differenz zwischen Geräten höchstens ein Tick = 200 ms real (12 s virtuell bei 60)
- Die Differenz ist ein Abtastphasen-Effekt, kein Drift: sie springt zwischen zwei Werten (z.B. +2,3 s / -9,8 s), wächst nicht über die Zeit
- Uhr selbst (Master-Skew) liegt bei wenigen ms: Abtastlatenz in us mal Tempo

Alle Samples: [diag-pv-1.log](logs/diag-pv-1.log), [diag-battery-1.log](logs/diag-battery-1.log), [diag-pv-2.log](logs/diag-pv-2.log) (Marker `SAMPLE n label realMs=...`).

## Bekannte Grenzen

- Skew nur auf einem Rechner, Loopback
- Header-Zeit hat Tick-Auflösung (200 ms real); die Uhr der Geräte genauer als das Register
- Vor `start` laufen Geräte schon als `State Running` mit Leistung, Uhr `Stopped`
- `effect:` direkt nach `write limit` zeigt noch die alte Leistung (Zustand wird erst im nächsten Tick neu gerechnet), erst der nächste `header` zeigt die Wirkung
- Das Skript braucht `set -m`, sonst ignorieren Hintergrundprozesse SIGINT und das Wetter beendet nur mit SIGTERM (Exit 143)
- Läuft ein fremder Master auf 1502, melden sich die Geräte dort an: Master-Port per `MASTER_PORT` setzen
- Clock-Endpoint-Register 18..27 wurden in diesem Lauf nicht direkt gelesen, nur Master-Skew und Header

AI-USAGE: Claude Sonnet 5.5: half bei der Formatierung/Struktur dieses Markdown-Dokuments (Tabellen, Auswertung der Logs)
