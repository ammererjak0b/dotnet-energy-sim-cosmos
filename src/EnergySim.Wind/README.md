# EnergySim.Wind

Windkraftmodul für die gemeinsame .NET-10-Solution. Verantwortlich: Jonas Kreibich.

## Aktueller Umfang

Das Modul enthält `WindOptions`, das reine Anlagenmodell `WindPlant` und den
fachlichen Zustand `WindPlantStatus`. Leistungskurve, kumulierte Energie,
Sturmschutz, Ein/Aus, Leistungslimit und technische Störung sind implementiert.
Die Bibliothek ist in der Solution und im gemeinsamen xUnit-Testprojekt eingebunden.
Der Geräteadapter und die CLI-Registrierung folgen im nächsten Schritt.
`--device wind` ist deshalb noch nicht verfügbar.

## Aufbau und Schnittstellen

- `WindOptions` ist eine reine Konfigurationsklasse ohne I/O oder Zugriff auf eine Uhr.
- `validate()` prüft die Werte und wirft bei ungültiger Konfiguration eine
  `ArgumentOutOfRangeException`. `ParamName` bezeichnet die betroffene Property.
- `SECTION_NAME` enthält `Wind` für die spätere Konfigurationsbindung. Eine Bindung
  durch die CLI ist in diesem Schritt noch nicht implementiert.
- Das Projekt referenziert ausschließlich `EnergySim.Core`. Die .NET-Einstellungen
  kommen aus der gemeinsamen `Directory.Build.props`.
- `WindPlant` prüft die Optionen beim Erstellen und speichert eine private Kopie.
  Spätere Änderungen am ursprünglichen Optionsobjekt verändern die Anlage nicht.
- `update(TimeSpan step, WeatherDto? weather)` verarbeitet die vom Aufrufer
  vorgegebene virtuelle Dauer. Das Modell besitzt keine Uhr und führt kein I/O aus.
  Ein absoluter Zeitstempel wird für den kumulierten Energiezähler nicht benötigt.

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

## Bauen und testen

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet build EnergySim.sln
dotnet test tests/EnergySim.Tests/EnergySim.Tests.csproj --filter FullyQualifiedName~EnergySim.Tests.Wind
dotnet test EnergySim.sln
```

Die 87 Wind-Testfälle prüfen Optionen sowie Leistungskurve, virtuelle Energiebilanz,
Grenzwerte, Sturm-Wiederanlauf, Wetterausfall, Befehle und Neustart. Sie verwenden
fest erzeugte `WeatherDto`-Werte und Zeitspannen ohne externe Wetterquelle,
Netzwerk oder reale Wartezeit.

## Nächster Schritt und Einschränkungen

Als Nächstes verbindet ein `WindDevice` das Modell mit dem gemeinsamen
Gerätevertrag, der Anzeige und den Befehlen. Die Auswahl in der CLI wird erst
danach ergänzt.

Die vorhandenen Wetterwerte `WeatherDto.WindSpeedMs` beziehen sich auf 10 m Höhe.
Eine Umrechnung auf Nabenhöhe ist nicht implementiert. Es gibt noch keinen
Modbus-Server oder Registerplan für Wind; der spätere Registerkopf muss mit den
gemeinsamen Teamkonventionen abgestimmt werden.
