# EnergySim.Wind

Windkraftmodul für die gemeinsame .NET-10-Solution. Verantwortlich: Jonas Kreibich.

## Aktueller Umfang

Das Modul enthält `WindOptions`, das reine Anlagenmodell `WindPlant` und den
fachlichen Zustand `WindPlantStatus`. Leistungskurve, kumulierte Energie,
Sturmschutz, Ein/Aus, Leistungslimit und technische Störung sind implementiert.
Die Bibliothek ist in der Solution und im gemeinsamen xUnit-Testprojekt eingebunden.
`WindDevice` verbindet das Modell mit dem gemeinsamen Gerätevertrag, der Anzeige
und den Befehlen. Die Anlage ist über `--device wind` und die Visual-Studio-Profile
`Wind` sowie `Wind (3600x)` startbar.

## Aufbau und Schnittstellen

- `WindOptions` ist eine reine Konfigurationsklasse ohne I/O oder Zugriff auf eine Uhr.
- `validate()` prüft die Werte und wirft bei ungültiger Konfiguration eine
  `ArgumentOutOfRangeException`. `ParamName` bezeichnet die betroffene Property.
- `SECTION_NAME` enthält `Wind`; die CLI bindet diesen Konfigurationsabschnitt
  über `DeviceFactory.bindWindOptions`.
- Das Projekt referenziert ausschließlich `EnergySim.Core`. Die .NET-Einstellungen
  kommen aus der gemeinsamen `Directory.Build.props`.
- `WindPlant` prüft die Optionen beim Erstellen und speichert eine private Kopie.
  Spätere Änderungen am ursprünglichen Optionsobjekt verändern die Anlage nicht.
- `update(TimeSpan step, WeatherDto? weather)` verarbeitet die vom Aufrufer
  vorgegebene virtuelle Dauer. Das Modell besitzt keine Uhr und führt kein I/O aus.
  Ein absoluter Zeitstempel wird für den kumulierten Energiezähler nicht benötigt.
- `WindDevice` erbt von `SimulatedDeviceBase` und meldet `NeedsWeather = true`.
  Der gemeinsame Loop liefert Wetter und virtuelle Dauer. `WindSpeedMs`,
  `AvailablePowerKw` und `OperatingStatus` stehen als typisierte Gerätewerte bereit.

## Starten und bedienen

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet run --project src/EnergySim.Cli -- --device wind
dotnet run --project src/EnergySim.Cli -- --device wind --id wind-2 --location salzburg --start 2026-10-23T13:00:00Z
dotnet run --project src/EnergySim.Cli -- --device wind --csv wind-run.csv
```

Der zweite Aufruf verwendet einen geprüften Zeitpunkt mit ausreichend Wind aus
dem lokalen Referenzjahr. Beim Standardstart kann Wind unterhalb der Einschaltgrenze
liegen: `Standby` mit 0 W ist dann korrekt und keine Fehlfunktion.

In Visual Studio `EnergySim.Cli` als Startprojekt wählen und oben das Profil `Wind`
oder `Wind (3600x)` auswählen. Das Profil `Pick from menu` bietet ebenfalls `wind` an.

Gemeinsame Gerätetasten: `O` ein, `F` aus, `L` Leistungslimit, `T` technische Störung,
`R` Fehlerreset. Uhr/Programm: `P` Pause/Fortsetzen, `V` Geschwindigkeit setzen,
`+`/`-` schneller/langsamer, `Q` beenden. Wind belegt keine zusätzlichen Tasten.
Bei umgeleiteter Ein-/Ausgabe beendet eine Zeile `quit` oder das Ende der Eingabe
den Lauf. Mehrere lokale Instanzen erhalten unterschiedliche IDs und CSV-Pfade.
Erzeugte CSV-Dateien werden durch `.gitignore` aus der Versionsverwaltung gehalten.

Der aktuelle lokale Betrieb verwendet `LocalClock`, `LocalWeatherClient` und
die vorhandenen Wetterdateien. Es müssen keine anderen Simulatoren oder Dienste
laufen, und es wird zur Laufzeit keine Internetverbindung benötigt.

## Gemeinsamer Wetterprozess nach Zusammenführung auf main

Wind verwendet bereits den gemeinsamen Wettervertrag: `NeedsWeather = true`,
`IWeatherClient` und `WeatherDto.WindSpeedMs` in m/s. Der gemeinsame Loop fragt
Wetter mit Standort und virtueller UTC-Zeit ab und übergibt es an `WindDevice`.
Das Windprojekt benötigt keinen eigenen HTTP-Client und keinen Projektverweis
auf den Wetterservice. Windrichtung ist im Wetterobjekt vorhanden, wird vom
aktuellen Anlagenmodell aber nicht zur Leistungsberechnung verwendet.

Die HTTP-Anbindung gehört zum gemeinsamen Wetterbeitrag. Als Referenz wurde
Jakobs Commit `4b5f43e` nur gelesen. Dieser Wind-Schritt übernimmt oder dupliziert
keine Wetterservice-, HTTP-Client- oder CLI-Wetterimplementierung.

**Voraussetzung für die folgenden Aufrufe:** Wind und der gemeinsame Wetterbeitrag
müssen auf `main` zusammengeführt sein. Auf dem aktuellen Wind-Ausgangsstand
`18de2eb` gibt es noch keine Auswertung von `--weather-url`; die Option allein
schaltet dort nicht auf HTTP um. Die bestehenden lokalen Startprofile bleiben
daher unverändert.

Nach der Zusammenführung aus dem Repository-Hauptverzeichnis zwei Terminals öffnen:

```powershell
dotnet run --project src/EnergySim.WeatherService
```

```powershell
dotnet run --project src/EnergySim.Cli -- --device wind --weather-url http://localhost:5200 --location salzburg --start 2026-10-23T13:00:00Z --speed 60
```

Der Standardport ist 5200. `localhost` bezeichnet den eigenen Rechner; bei einem
Wetterprozess auf einem anderen Rechner dessen Host/IP einsetzen. Ohne
`--weather-url` bleibt der lokale Betrieb möglich. Der gemeinsame HTTP-Client
rundet die Abfragezeit auf die volle UTC-Minute ab. Bei Vergleichen mit lokalem
Wetter dieselbe volle Minute verwenden. Die vorhandenen Referenzdaten erzeugen
auch über HTTP keine zusätzlichen Windböen; der gewählte Zeitpunkt liefert
7,5 m/s und mit den Wind-Standardoptionen ungefähr 23,2 kW.

| Wetterergebnis des gemeinsamen Clients | Verhalten von Wind und gemeinsamer Anzeige |
|---|---|
| Noch kein gültiger Wert | Leistung 0, Windstatus `WeatherUnavailable`, Wetterwarnung |
| Letzter gültiger Wert mit `IsStale = true` | Berechnung mit diesem Wert läuft weiter; gemeinsames Dashboard warnt |
| Wieder frische Werte | Aktuelle Windwerte werden verwendet; Wetterwarnung verschwindet |

Eine Stale-Warnung ist keine technische Anlagenstörung. Bei ausreichend Wind
im letzten gültigen Wert kann Wind weiterhin `Running` anzeigen, während das Dashboard eine
Wetterwarnung zeigt. Die bestehenden Beschränkungen der gemeinsamen Headless-Ausgabe
gelten auch für Wind.

Beim Zusammenführen nach `main` müssen die Wind-Auswahl in `DEVICE_NAMES`, die
Wind-Factory und Projektverweise mit `WeatherUrl`, HTTP-Client-Auswahl und dem
Wetterservice-Projekt erhalten bleiben. Der CLI-Hilfetext muss sowohl `wind`
als auch `--weather-url` nennen; die dynamische Geräteliste aus dem Wind-Beitrag
beibehalten und um den Wetter-URL-Hinweis ergänzen.

Die Tests der Wetterübergabe und Warnungen sind unter
`tests/EnergySim.Tests/Wind/WindWeatherIntegrationTests.cs` nachgewiesen. Sie
verwenden den echten Windadapter und Loop mit einem Fake-`IWeatherClient`, keine
HTTP-Verbindung. Den Lauf mit echtem Wetterprozess nach der Zusammenführung
separat prüfen: positive Leistung, Prozess stoppen, Stale-Warnung beobachten,
Prozess neu starten und Rückkehr zu frischen Daten prüfen.

## Konfiguration

Die Standardwerte sind Demonstrationswerte und keine Herstellerdaten einer realen
Windkraftanlage. Alle Geschwindigkeiten sind in Metern pro Sekunde angegeben.

| Property | Standard | Bedeutung |
|---|---:|---|
| `RatedPowerKw` | 100 kW | Elektrische Nennleistung |
| `CutInWindSpeedMs` | 3 m/s | Beginn der Erzeugung oberhalb dieser Grenze |
| `RatedWindSpeedMs` | 12 m/s | Beginn der Nennleistung |
| `CutOutWindSpeedMs` | 25 m/s | Sturmabschaltung ab dieser Grenze |
| `RestartWindSpeedMs` | 20 m/s | Wiederanlauf nach Sturm erst unter dieser Grenze |

Gültige Werte erfüllen:

- Alle Werte sind endlich: weder NaN noch positive oder negative Unendlichkeit.
- `RatedPowerKw > 0`.
- `0 <= CutInWindSpeedMs < RatedWindSpeedMs < CutOutWindSpeedMs`.
- `CutInWindSpeedMs < RestartWindSpeedMs < CutOutWindSpeedMs`.

Eine Wiederanlaufgrenze unterhalb der Nennwindgeschwindigkeit ist zulässig. Die
Prüfung schreibt keine zusätzliche Beziehung zwischen diesen beiden Grenzen vor.
Sie verändert die Werte nicht. Nach Änderungen an den Properties muss der spätere
Aufrufer `validate()` erneut ausführen, bevor er die Konfiguration verwendet.

Die Werte können unter `Wind` in `src/EnergySim.Cli/appsettings.json` stehen oder
als Argumente übergeben werden, beispielsweise `--Wind:RatedPowerKw 250`.
Ein vollständiger Beispielabschnitt:

```json
{
  "Wind": {
    "RatedPowerKw": 100,
    "CutInWindSpeedMs": 3,
    "RatedWindSpeedMs": 12,
    "CutOutWindSpeedMs": 25,
    "RestartWindSpeedMs": 20
  }
}
```

Die gemeinsame Konfigurationsdatei wurde für die Wind-Einbindung nicht verändert;
fehlende Werte verwenden weiterhin die Klassenstandardwerte. Ungültige
Anlagenwerte werden beim Start mit Verweis auf den Abschnitt `Wind` abgelehnt.

## Leistung und Energie

Bei Wind bis einschließlich Einschaltgrenze ist die verfügbare Leistung 0.
Zwischen Einschalt- und Nennwind gilt die vereinfachte Lehrmodell-Kurve:

`P = RatedPowerKw * (v³ - CutInWindSpeedMs³) / (RatedWindSpeedMs³ - CutInWindSpeedMs³)`

Die Implementierung verwendet eine algebraisch äquivalente normierte Form ohne
das direkte Kubieren großer Geschwindigkeiten. Zwischen Nennwind und Abschaltwind
wird die verfügbare Leistung auf die Nennleistung begrenzt. Ab Abschaltwind ist
sie 0. Es handelt sich nicht um eine Herstellerkennlinie.

`AvailablePowerKw` zeigt die Leistung aus der aktuellen Kurve vor Ein/Aus,
technischer Störung, gespeicherter Sturmsperre und externer Leistungsbegrenzung.
`PowerKw` ist die tatsächlich abgegebene elektrische Leistung. Sie ist als
Erzeugungsleistung nicht negativ. Das Leistungslimit ist eine Obergrenze relativ
zur Nennleistung; es kann keine zusätzliche Windleistung erzeugen.

`TotalEnergyWh` wächst um `PowerKw * 1000 * step.TotalHours`. Der aktuelle
Leistungswert wird über die gesamte übergebene Dauer als konstant angenommen.
Der Zähler verwendet `double` und rundet nicht nach jedem Schritt. Es gibt bisher
keinen Tageszähler und keine Aufteilung eines Schritts auf Wetterwechsel.

Ein Schritt von 0 aktualisiert Wetter, Leistung und Schutzstatus, addiert aber
keine Energie. Negative Schritte werden vor jeder Zustandsänderung abgelehnt.
Der Geschwindigkeitsfaktor wird nicht nochmals im Modell angewendet.

## Zustände, Bedienung und Wetterausfall

- Die Anlage startet eingeschaltet mit Limit 100 %, ohne Wetter und ohne Energie.
- `turnOff()` stoppt die Leistung sofort; `turnOn()` berechnet sie aus dem zuletzt
  gültigen Wetter neu. Die Befehle verändern den Energiezähler nicht.
- `setPowerLimit(percent)` akzeptiert nur endliche Werte von 0 bis 100 und wirkt
  sofort. Liegt die verfügbare Leistung bereits unter der Grenze, bleibt sie gleich.
- Ab `CutOutWindSpeedMs` wird `IsStormShutdown` gesetzt. Die Sperre bleibt bis
  zu einer gültigen Geschwindigkeit strikt unter `RestartWindSpeedMs` bestehen.
  Genau auf der Wiederanlaufgrenze bleibt die Anlage gesperrt.
- `triggerFault()` speichert eine technische Störung. `resetFault()` setzt nur
  diese Störung zurück und hebt keine Sturmsperre auf.
- Fehlendes Wetter oder negative, unendliche beziehungsweise NaN-Windwerte ergeben
  `HasWeather = false` und Leistung 0. Gesammelte Energie und Sturmsperre bleiben
  erhalten. Gültige neue Werte ermöglichen die weitere Berechnung.
- `reset()` beginnt einen neuen Lauf: eingeschaltet, Limit 100 %, keine Störung,
  keine Sturmsperre, kein Wetter und Energie 0.

`WindPlantStatus` unterscheidet `Off`, `Standby`, `Running`, `Limited`, `Fault`,
`StormShutdown` und `WeatherUnavailable`. Bei mehreren Ursachen hat eine technische
Störung Vorrang, danach Aus, fehlendes Wetter und Sturmsperre. Ein Limit von 0 %
bei vorhandenem Wind ergibt `Limited`; Windstille ohne diese Ursachen `Standby`.

## Gemeinsamer Gerätekopf, Anzeige und CSV

Der Gerätetyp ist `DeviceType.Wind = 4`; die bisherigen Gerätetypwerte bleiben
unverändert. `ActivePowerW` enthält gerundete erzeugte Watt, `EnergyWh` die
gerundeten kumulierten Wattstunden. Bruchteile bleiben im Modell erhalten.

Der Adapter bildet Off, Running, Limited und Fault direkt auf den gemeinsamen
`DeviceState` ab. Windstille, Sturmabschaltung und Wetterausfall erscheinen im
Kopf als `Standby`. Der zusätzliche Status zeigt den fachlichen Grund:

| Situation | Anzeige | Schweregrad | Fehlercode |
|---|---|---|---:|
| Technische Störung | `Technical fault (code 1)` | Fault | 1 |
| Sturmabschaltung | `Storm shutdown` | Warning | 0 |
| Fehlendes/ungültiges Wetter | `Weather unavailable`, Wetterwerte `n/a` | Warning | 0 |

Die sechs Anzeige-/CSV-Spalten sind immer `Power`, `Wind speed`, `Available power`,
`Status`, `Total energy` und `Fault`. Der Leistungsbalken bezieht sich auf die
Nennleistung. Formatierung verwendet die gemeinsamen Hilfsmethoden; es gibt
keinen eigenen Wind-CSV-Schreiber und keine eigene Terminalimplementierung.

## Bauen und testen

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet build EnergySim.sln
dotnet test tests/EnergySim.Tests/EnergySim.Tests.csproj --filter FullyQualifiedName~EnergySim.Tests.Wind
dotnet test EnergySim.sln
```

Die 116 Wind-Testfälle prüfen Optionen, Modell und Adapter einschließlich
Gerätekopf, Einheiten, Warnungen, Befehlen, Neustart und Rundung. Dazu kommen
CLI-/Factory-Tests und Wind als weiterer Fall der gemeinsamen Gerätetests. Sie verwenden
fest erzeugte `WeatherDto`-Werte und Zeitspannen ohne externe Wetterquelle,
Netzwerk oder reale Wartezeit. Sechs Integrationstests verbinden zusätzlich
das echte Windgerät und den gemeinsamen Loop mit kontrolliertem Wetter und
`FakeTimeProvider`. Sie prüfen Standortübergabe, virtuelle Energie, fehlende Werte,
Stale-Warnung und Wiederherstellung. HTTP-Transport und Cache des gemeinsamen
Clients werden damit nicht nachgebaut oder als getestet ausgewiesen.

## Nächster Schritt und Einschränkungen

Prüfstand 25.10.2026: Die gesamte Solution baut mit 0 Warnungen/Fehlern.
Alle 116 Wind-Testfälle sind grün; im Gesamtlauf bestehen 463 von 468 Fällen.
Die fünf bereits vor Wind bestehenden CSV-Dateisperrfehler bleiben. Die Prüfung
der Wind-Wettervorbereitung steht unter `docs/evidence/wind-weather-contract.md`.
Ein echter lokaler Lauf mit Wetter, positiver Leistung, CSV und kontrolliertem
Beenden ist unter `docs/evidence/wind-local-run.log` dokumentiert.

Die vorhandenen Wetterwerte `WeatherDto.WindSpeedMs` beziehen sich auf 10 m Höhe.
Eine Umrechnung auf Nabenhöhe ist nicht implementiert. Es gibt noch keinen
Modbus-Server oder Registerplan für Wind; der spätere Registerkopf muss mit den
gemeinsamen Teamkonventionen abgestimmt werden.
