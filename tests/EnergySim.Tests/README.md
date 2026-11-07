# EnergySim.Tests

Gemeinsames xUnit-Testprojekt für Core, Wetter, Terminal, CLI und Gerätemodule.
Die jeweiligen Modulverantwortlichen pflegen die zugehörigen Tests; verantwortlich
für den neuen Bereich `Wind` ist Jonas Kreibich.

## Voraussetzungen und Verwendung

Benötigt werden das gemeinsame .NET-10-SDK und die in der Projektdatei festgelegten
NuGet-Pakete. Nach dem Wiederherstellen der Pakete sollen die Tests ohne
Internetverbindung ausführbar sein.

Aus dem Repository-Hauptverzeichnis:

```powershell
dotnet test EnergySim.sln
dotnet test tests/EnergySim.Tests/EnergySim.Tests.csproj --filter FullyQualifiedName~EnergySim.Tests.Wind
```

## Aufbau und Konventionen

Die Unterordner entsprechen den getesteten Modulen. Wind verwendet
`Wind/WindOptionsTests.cs` für Demonstrationsstandardwerte, den späteren
Konfigurationsabschnitt und die Validierung der Anlagenparameter.
`Wind/WindPlantTests.cs` prüft die Leistungskurve, Energie über virtuelle
Zeitspannen, Wetterausfall, Sturm-Wiederanlauf, Befehle und Neustart. Die
Wetterobjekte sind feste Testeingaben; eine externe Quelle wird nicht verwendet.
`Wind/WindDeviceTests.cs` prüft den gemeinsamen Gerätekopf, Befehle, Anzeige,
Warnungen, Wind-Details und Rundung. Wind ist außerdem in den gemeinsamen
Gerätebefehlsprüfungen sowie in CLI-/Factory-Tests enthalten. Die Hilfeprüfung
verwendet eine nicht parallele Testkollektion zum sicheren Umleiten von Console.Out.

`Wind/WindWeatherIntegrationTests.cs` verbindet das echte Windgerät mit dem
gemeinsamen `DeviceSimulationLoop`. `WindWeatherLoopHarness` verwendet die
vorhandenen Wetter-/Uhr-Fakes, einen `FakeTimeProvider` und einen Kanal zur
Übergabe fertiger Frames. Die sechs Fälle prüfen Standort und virtuelle Energie,
fehlendes Wetter, erste Wetterwerte nach Ausfall, Stale-Warnung, frische Werte
nach Stale und Erhalt der Energie bei später fehlendem Wetter. Die Tests benötigen
keinen HTTP-Client oder Wetterservice aus einem anderen Branch. Sie prüfen die
Wind-Seite des vorhandenen Vertrags, nicht HTTP-Transport oder Client-Cache.

Neue Tests folgen dem Schema `MethodName_Condition_ExpectedResult` mit getrennten
Arrange-, Act- und Assert-Blöcken. Für Wertetabellen werden `[Theory]` und
`[InlineData]` verwendet. Neue Logik wird erst nach einem fehlgeschlagenen Test
implementiert. Für Zeit- oder Kommunikationsverhalten sind kontrollierbare Fakes
zu verwenden; neue Tests warten nicht auf die reale Uhr und greifen nicht auf das
Internet zu.

## Einschränkungen

Die Windtests prüfen Optionen, Anlagenmodell, Geräteadapter und Wetterübergabe
über den gemeinsamen Loop. Dazu
kommen CLI-/Factory-Prüfungen. Ein echter lokaler CLI-Lauf mit Wetter und CSV ist
unter `docs/evidence/wind-local-run.log` dokumentiert. Ein Modbus-Server und
entsprechende Netzwerk-Integrationstests sind noch nicht implementiert.

Prüfstand 25.10.2026 unter Windows: Alle 116 Wind-Testfälle sind grün. Im gesamten
Projekt bestehen fünf Fehler in `CsvHistoryWriterTests` beim Lesen noch zum
Schreiben geöffneter Dateien. Dieselben fünf Fehler sind am unveränderten
Ausgangscommit `5e10b95` reproduziert. Der gesamte Testlauf ist daher noch nicht
grün; diese bestehenden Tests wurden in den Wind-Schritten nicht geändert.
Aktueller Gesamtlauf: 463 von 468 Testfällen erfolgreich, fünf bekannte CSV-Fehler.
Die aktuelle Prüfung steht unter `docs/evidence/wind-weather-contract.md`; ein echter HTTP-Lauf
mit Wind folgt erst nach Zusammenführung beider Beiträge auf `main`.
