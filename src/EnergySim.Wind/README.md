# EnergySim.Wind

Windkraftmodul für die gemeinsame .NET-10-Solution. Verantwortlich: Jonas Kreibich.

## Aktueller Umfang

Der erste Schritt enthält `WindOptions` mit Standardwerten und Validierung. Die
Bibliothek ist in der Solution und im gemeinsamen xUnit-Testprojekt eingebunden.
Eine Leistungskurve, Energieberechnung und der Geräteadapter sind noch nicht
implementiert. `--device wind` ist deshalb noch nicht verfügbar.

## Aufbau und Schnittstellen

- `WindOptions` ist eine reine Konfigurationsklasse ohne I/O oder Zugriff auf eine Uhr.
- `validate()` prüft die Werte und wirft bei ungültiger Konfiguration eine
  `ArgumentOutOfRangeException`. `ParamName` bezeichnet die betroffene Property.
- `SECTION_NAME` enthält `Wind` für die spätere Konfigurationsbindung. Eine Bindung
  durch die CLI ist in diesem Schritt noch nicht implementiert.
- Das Projekt referenziert ausschließlich `EnergySim.Core`. Die .NET-Einstellungen
  kommen aus der gemeinsamen `Directory.Build.props`.

## Konfiguration

Die Standardwerte sind Demonstrationswerte und keine Herstellerdaten einer realen
Windkraftanlage. Alle Geschwindigkeiten sind in Metern pro Sekunde angegeben.

| Property | Standard | Bedeutung |
|---|---:|---|
| `RatedPowerKw` | 100 kW | Elektrische Nennleistung |
| `CutInWindSpeedMs` | 3 m/s | Geplanter Beginn der Erzeugung |
| `RatedWindSpeedMs` | 12 m/s | Geplanter Beginn der Nennleistung |
| `CutOutWindSpeedMs` | 25 m/s | Geplante Sturmabschaltgrenze |
| `RestartWindSpeedMs` | 20 m/s | Geplante Wiederanlaufgrenze nach Sturm |

Gültige Werte erfüllen:

- Alle Werte sind endlich: weder NaN noch positive oder negative Unendlichkeit.
- `RatedPowerKw > 0`.
- `0 <= CutInWindSpeedMs < RatedWindSpeedMs < CutOutWindSpeedMs`.
- `CutInWindSpeedMs < RestartWindSpeedMs < CutOutWindSpeedMs`.

Eine Wiederanlaufgrenze unterhalb der Nennwindgeschwindigkeit ist zulässig. Die
Prüfung schreibt keine zusätzliche Beziehung zwischen diesen beiden Grenzen vor.
Sie verändert die Werte nicht. Nach Änderungen an den Properties muss der spätere
Aufrufer `validate()` erneut ausführen, bevor er die Konfiguration verwendet.

## Bauen und testen

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet build EnergySim.sln
dotnet test tests/EnergySim.Tests/EnergySim.Tests.csproj --filter FullyQualifiedName~EnergySim.Tests.Wind
dotnet test EnergySim.sln
```

Die Windtests prüfen Standardwerte, Abschnittsname, gültige Konfigurationen und
ungültige Leistungs-/Windwerte einschließlich Grenzgleichheit, NaN und
Unendlichkeit. Sie benötigen kein Wetter, Netzwerk und keine reale Wartezeit.

## Nächster Schritt und Einschränkungen

Als Nächstes wird `WindPlant` testgetrieben entwickelt. Dafür ist eine vereinfachte
Leistungskurve mit Erzeugungsbereich, Nennleistung, Sturmabschaltung und getrennter
Wiederanlaufgrenze geplant. Anschließend verbindet ein `WindDevice` das Modell mit
dem gemeinsamen Gerätevertrag, der Anzeige und den Befehlen.

Die vorhandenen Wetterwerte `WeatherDto.WindSpeedMs` beziehen sich auf 10 m Höhe.
Eine Umrechnung auf Nabenhöhe ist nicht implementiert. Es gibt noch keinen
Modbus-Server oder Registerplan für Wind; der spätere Registerkopf muss mit den
gemeinsamen Teamkonventionen abgestimmt werden.
