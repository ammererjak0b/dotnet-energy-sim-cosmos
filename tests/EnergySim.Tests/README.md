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

Neue Tests folgen dem Schema `MethodName_Condition_ExpectedResult` mit getrennten
Arrange-, Act- und Assert-Blöcken. Für Wertetabellen werden `[Theory]` und
`[InlineData]` verwendet. Neue Logik wird erst nach einem fehlgeschlagenen Test
implementiert. Für Zeit- oder Kommunikationsverhalten sind kontrollierbare Fakes
zu verwenden; neue Tests warten nicht auf die reale Uhr und greifen nicht auf das
Internet zu.

## Einschränkungen

Die Windtests prüfen Optionen und das reine Anlagenmodell. Es gibt noch keinen
Wind-Geräteadapter und keine entsprechenden CLI-/Modbus-Integrationstests.
Erfolgreiche Modelltests bestätigen diese späteren Funktionen nicht.

Prüfstand 24.10.2026 unter Windows: Alle 87 Wind-Testfälle sind grün. Im gesamten
Projekt bestehen fünf Fehler in `CsvHistoryWriterTests` beim Lesen noch zum
Schreiben geöffneter Dateien. Dieselben fünf Fehler sind am unveränderten
Ausgangscommit `5e10b95` reproduziert. Der gesamte Testlauf ist daher noch nicht
grün; diese bestehenden Tests wurden in den Wind-Schritten nicht geändert.
Aktueller Gesamtlauf: 423 von 428 Testfällen erfolgreich, fünf bekannte CSV-Fehler.
