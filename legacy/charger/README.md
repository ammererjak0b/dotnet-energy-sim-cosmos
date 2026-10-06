# Dual-Port Charger Simulator

Eine C#-Konsolenanwendung zur Simulation einer Dual-Port-Ladestation für Elektrofahrzeuge.

Die Simulation kann zwei Fahrzeuge gleichzeitig laden. Dabei werden die maximale Leistung der Ladestation, die Leistung der einzelnen Ports, die maximale Ladeleistung der Fahrzeuge und der aktuelle Ladezustand berücksichtigt.

## Funktionsweise

- Die Simulation arbeitet mit festen Simulationsschritten.
- Die Simulationszeit wird bei jedem Schritt weitergeführt.
- Bis einschließlich 80 % SOC wird mit der maximal möglichen Ladeleistung geladen.
- Oberhalb von 80 % wird die Ladeleistung reduziert.
- Die verfügbare Gesamtleistung wird bei zwei Fahrzeugen fair aufgeteilt.
- Die Ladung endet exakt bei 100 % SOC.
- Nach Erreichen von 100 % liefert der entsprechende Port 0 kW.
- Die Simulation speichert für jeden Schritt einen Snapshot der aktuellen Zustände.
- Die Snapshot-Daten können als CSV-Datei exportiert werden.

Die Simulation unterscheidet zwischen:

- `ON` – Charger ist aktiv
- `OFF` – Charger ist ausgeschaltet, die Simulationszeit läuft weiter, aber es wird nicht geladen
- `FAULT` – Charger oder Port ist gestört
- `PAUSED` – die Simulation steht vollständig still

## Start

Die Anwendung kann mit folgendem Befehl gestartet werden:
'dotnet run'


Beim Start kann zwischen der Standardkonfiguration und einer manuellen Konfiguration gewählt werden. Bei der manuellen Konfiguration können unter anderem Charger-Leistung, Port-Leistungen und Fahrzeugdaten eingegeben werden.

## Bedienung

| Taste | Funktion |
|---|---|
| `P` | Simulation pausieren |
| `R` | Simulation fortsetzen |
| `O` | Charger ein- oder ausschalten |
| `F` | Fehlerverwaltung öffnen |
| `C` | Simulationsparameter konfigurieren |
| `N` | Neues Fahrzeug hinzufügen |
| `A` | Fahrzeug mit einem Port verbinden |
| `D` | Fahrzeug von einem Port trennen |
| `L` | Fahrzeugliste anzeigen |
| `E` | Simulationsdaten als CSV exportieren |
| `X` | Simulation neu starten |
| `Q` | Anwendung beenden |

## Fehlerverwaltung

Mit `F` kann zwischen folgenden Optionen gewählt werden:

- Charger-Fehler setzen
- Fehler für Port 1 setzen
- Fehler für Port 2 setzen
- Charger-Fehler beheben
- Fehler für Port 1 beheben
- Fehler für Port 2 beheben

Bei einem Charger-Fehler werden beide Ports als `OFF` angezeigt. Bei einem Fehler eines einzelnen Ports kann der andere Port weiterhin laden.

## Simulationsparameter

Mit `C` können folgende Parameter geändert werden:

1. Simulationsschrittweite in Minuten
2. Anzeigeverzögerung in Millisekunden

Nach der Konfiguration bleibt die Simulation pausiert. Mit `R` kann sie fortgesetzt werden.

## CSV-Export

Mit `E` wird die bisherige Snapshot-Historie als CSV-Datei exportiert.

Die CSV-Datei enthält unter anderem:

- Simulationszeit
- Charger-Status und Charger-Fehler
- Status und Fehler der einzelnen Ports
- Fahrzeug-ID und Fahrzeugname pro Port
- aktuelle Ladeleistung
- kumulierte Energie
- SOC der Fahrzeuge
- Gesamtleistung
- Gesamtenergie

Die CSV-Datei kann anschließend beispielsweise mit Excel oder anderen Analysewerkzeugen verwendet werden.