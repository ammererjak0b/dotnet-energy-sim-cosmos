# Dual-Port Charger Simulator

Eine C#-Konsolenanwendung zur Simulation einer Dual-Port-Ladestation für Elektrofahrzeuge. Die Anwendung modelliert einen Charger mit zwei Ladeports, an denen bis zu zwei Fahrzeuge gleichzeitig geladen werden können.

## Voraussetzungen

- .NET 10 SDK
- Eine Konsole beziehungsweise ein Terminal
- Windows, Linux oder macOS mit installierter .NET-10-Laufzeit

Die installierte .NET-Version kann mit folgendem Befehl geprüft werden:

```text
dotnet --version
```

## Start

1. Repository im Terminal öffnen.
2. In das Verzeichnis wechseln, in dem `LB_01.csproj` liegt.
3. Anwendung starten:

```text
dotnet run
```

Optional kann das Projekt vorher gebaut werden:

```text
dotnet build
```

Beim Start wird gefragt, ob die Standardkonfiguration verwendet werden soll. Mit `Y` oder einfach mit der Eingabetaste wird die Standardkonfiguration geladen. Mit einer anderen Eingabe kann eine manuelle Konfiguration vorgenommen werden.

## Bedienung

Die Anwendung läuft schrittweise. Während der Simulation können folgende Tasten verwendet werden:

| Taste | Funktion |
|---|---|
| `P` | Simulation pausieren |
| `R` | Simulation fortsetzen |
| `O` | Charger ein- oder ausschalten |
| `F` | Fehlerverwaltung öffnen |
| `C` | Simulationsschrittweite und Anzeigeverzögerung konfigurieren |
| `N` | Neues Fahrzeug hinzufügen |
| `A` | Fahrzeug mit einem Port verbinden |
| `D` | Fahrzeug von einem Port trennen |
| `L` | Fahrzeuge und Verbindungen anzeigen |
| `E` | Snapshot-Daten als CSV exportieren |
| `X` | Simulation neu starten |
| `Q` | Anwendung beenden |

Anwendungsbefehle wie `O`, `F`, `N`, `A`, `D`, `L` und `E` pausieren die Simulation während der Eingabe. Nach Abschluss kann die Simulation mit `R` fortgesetzt werden.

### Fehlerverwaltung

Mit `F` wird ein Menü zur Fehlerverwaltung geöffnet:

1. Charger-Fehler setzen
2. Fehler für Port 1 setzen
3. Fehler für Port 2 setzen
4. Charger-Fehler beheben
5. Fehler für Port 1 beheben
6. Fehler für Port 2 beheben
0. Menü schließen

Bei einem Charger-Fehler sind beide Ports betroffen. Bei einem Fehler an nur einem Port kann der andere Port weiterhin laden.

## Modellbeschreibung

Das Modell besteht aus einem `DualPortCharger`, zwei `ChargePort`-Objekten und bis zu zwei `ECar`-Objekten.

### Charger und Ports

- Der Charger besitzt eine maximale Gesamtleistung.
- Jeder der beiden Ports besitzt zusätzlich eine eigene maximale Leistung.
- Ein Port kann höchstens ein Fahrzeug aufnehmen.
- Ein Fahrzeug kann gleichzeitig nur mit einem Port verbunden sein.
- Der Charger startet im Status `ON`.
- Bei `OFF` läuft die Simulationszeit weiter, es wird aber keine Ladeleistung abgegeben.
- Bei `FAULT` wird der betroffene Charger beziehungsweise Port als fehlerhaft behandelt.

### Fahrzeuge und Ladevorgang

Für jedes Fahrzeug werden unter anderem folgende Werte modelliert:

- Batteriekapazität in kWh
- aktueller Ladezustand (SOC) in Prozent
- maximale Ladeleistung in kW
- Fahrzeug-ID und Name

Die tatsächlich verwendete Ladeleistung wird durch die kleinste verfügbare Grenze bestimmt: die maximale Ladeleistung des Fahrzeugs, die maximale Leistung des Ports und die noch verfügbare Gesamtleistung des Chargers. Bis einschließlich 80 % SOC wird mit der maximal möglichen Leistung geladen. Oberhalb von 80 % wird die Ladeleistung reduziert. Die Ladung endet exakt bei 100 % SOC.

Sind beide Ports aktiv, wird die verfügbare Gesamtleistung fair auf die Fahrzeuge verteilt. Nach Erreichen von 100 % SOC liefert der entsprechende Port 0 kW.

Für jeden Simulationsschritt wird ein Snapshot gespeichert. Dieser enthält den Zustand des Chargers, der Ports und der Fahrzeuge sowie Ladeleistung, Energieverbrauch und SOC.

### Zustände

- `ON` – Charger ist aktiv
- `OFF` – Charger ist ausgeschaltet; die Zeit läuft weiter, aber es wird nicht geladen
- `FAULT` – Charger oder Port ist gestört
- `PAUSED` – die Simulation steht vollständig still

## Beispielparameter

Die folgende Konfiguration entspricht der Standardkonfiguration:

| Parameter | Beispielwert |
|---|---:|
| Charger-Name | `Demo Charger` |
| Maximale Leistung Port 1 | `11 kW` |
| Maximale Leistung Port 2 | `11 kW` |
| Maximale Gesamtleistung | `22 kW` |
| Anzahl Fahrzeuge | `1` |
| Fahrzeugname | `Car 1` |
| Batteriekapazität | `60 kWh` |
| Initialer SOC | `60 %` |
| Maximale Ladeleistung des Fahrzeugs | `11 kW` |
| Simulationsschrittweite | `15 Minuten` |
| Anzeigeverzögerung | `1000 Millisekunden` |

Mit einer manuellen Konfiguration können beispielsweise zwei Fahrzeuge angelegt werden:

| Parameter | Fahrzeug 1 | Fahrzeug 2 |
|---|---:|---:|
| Batteriekapazität | `60 kWh` | `75 kWh` |
| Initialer SOC | `60 %` | `40 %` |
| Maximale Ladeleistung | `11 kW` | `7,4 kW` |

Bei zwei verbundenen Fahrzeugen wird die verfügbare Ladeleistung unter Berücksichtigung der Fahrzeug-, Port- und Charger-Grenzen aufgeteilt.

## Simulationsparameter

Mit `C` können während der Simulation folgende Parameter geändert werden:

1. Simulationsschrittweite in Minuten
2. Anzeigeverzögerung in Millisekunden

Nach der Konfiguration bleibt die Simulation pausiert. Mit `R` wird sie fortgesetzt.

## CSV-Export

Mit `E` wird die bisherige Snapshot-Historie als CSV-Datei in den Ordner `data` im Projektverzeichnis exportiert. Der Standarddateiname ist `simulation-data.csv`; alternativ kann ein eigener Dateiname eingegeben werden.

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
Ein Beispiel CSV-Export ist im Ordner `data` enthalten.

## Screenshots
Das folgende Bild zeigt die finale Version des Dashboards:

![alt text](../../img/Final_Dashboard.png)

Im folgenden Bild ist die Ladekurve eines Autos dargestellt, welches von 0% SOC auf 100% SOC mit maximal 11 kW Ladeleistung geladen wird:
![alt text](../../img/Ladekurve.png)