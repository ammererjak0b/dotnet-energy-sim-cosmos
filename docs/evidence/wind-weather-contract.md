# Wind: Vorbereitung für den gemeinsamen Wetterprozess

Prüfdatum: 25.10.2026. Umsetzung und Prüfung: Jonas Kreibich mit Codex.
Ausgangspunkt auf `jonas`: `18de2ebafd616a0fe8f165c43468c98bfd6078a9`.
Nur gelesener Wetter-Referenzstand: `origin/jakob`,
`4b5f43eaf0e290519be322eb9f060836bb2a7b80`.

## Umfang und Ergebnis

Das vorhandene `WindDevice` meldet `NeedsWeather = true` und verarbeitet bereits
`WeatherDto.WindSpeedMs`. Der gemeinsame Loop liefert Wetter und virtuelle Dauer.
Der geprüfte Wetterbeitrag wählt seinen HTTP-Client für alle wetterabhängigen
Geräte. Eine zusätzliche Wind-Produktivimplementierung war deshalb nicht nötig.

Ergänzt wurden sechs Integrationstests mit echtem `WindDevice` und echtem
`DeviceSimulationLoop`. Der Testaufbau verwendet die vorhandenen Wetter-/Uhr-Fakes,
einen `FakeTimeProvider` und einen Kanal für fertige Frames. Es gibt keine
Netzwerkverbindung, keinen Wetterserver und kein Warten auf reale Zeit.

| Testfall in `WindWeatherIntegrationTests` | Geprüftes Ergebnis |
|---|---|
| `RunAsync_FreshWeather_GeneratesFromSharedClient` | Standort Wien geht an Client; 12 m/s ergeben 100000 W und nach einer virtuellen Stunde 100000 Wh; erster Frame ohne Energiezuwachs |
| `RunAsync_NoWeather_KeepsGenerationAtZeroAndPublishesWarning` | 0 W/Wh, `WeatherUnavailable`, Wetterwarnung ohne technischen Windfehler |
| `RunAsync_FirstWeatherAfterOutage_StartsGenerationAndClearsWarning` | Nach Ausfall erste frische Werte: Erzeugung beginnt, Warnung verschwindet; ohne Wetter keine Energie gesammelt |
| `RunAsync_StaleWeather_ContinuesGenerationAndPublishesWarning` | Alter Clientwert wird weiter genutzt; Energie wächst und Warnung wird publiziert; kein technischer Windfehler |
| `RunAsync_FreshWeatherAfterStale_UsesNewWindAndClearsWarning` | Frische Windstille ersetzt alten Wind; Erzeugung stoppt, Energie bleibt, Warnung verschwindet |
| `RunAsync_WeatherBecomesUnavailable_StopsGenerationAndPreservesEnergy` | Späteres `Weather = null` stoppt Leistung und erhält den bisherigen Energiezähler |

Die fehlende Antwort nach vorherigem Erfolg ist ein allgemeiner Vertragstest.
Jakobs HTTP-Client liefert bei einem gewöhnlichen Ausfall stattdessen den letzten
gültigen Wert als stale. Dessen Cache wird hier nicht implementiert oder geprüft;
der Fake gibt nur das entsprechende `WeatherResult` vor.

## Ausgeführte Prüfungen

- Erster neuer Integrationstest allein: 1/1 bestanden, ohne Änderung am Produktivcode.
- Alle neuen Integrationstests: 6/6 bestanden.
- Gesamte Solution gebaut: 0 Warnungen, 0 Fehler.
- Alle Windtests: 116/116 bestanden.
- Gesamter Testlauf: 463/468 bestanden, keine übersprungenen Fälle.

Für Build und Tests wurden `MSBuildEnableWorkloadResolver=false`, ein einzelner
MSBuild-Prozess und deaktivierte Wiederverwendung der Buildprozesse verwendet.
Der Build lief ohne Restore; der vollständige Testlauf ohne erneuten Build/Restore.
Die Testberichte liegen als lokale TRX-Dateien außerhalb des Repositorys.

Die fünf Fehler des Gesamtlaufs sind die bereits vor Wind nachgewiesenen
Windows-Dateisperrfehler in `CsvHistoryWriterTests`:

- `PublishAsync_AfterInterval_WritesNextRow`
- `PublishAsync_ExistingFile_IsReplaced`
- `PublishAsync_FirstFrame_WritesHeaderFromFrameAndOneRow`
- `PublishAsync_VirtualTimeUnchanged_WritesNoRow`
- `PublishAsync_WithinInterval_WritesNoSecondRow`

Alle melden `System.IO.IOException` beim Lesen einer noch zum Schreiben offenen
CSV-Datei. Der Gesamtlauf ist damit nicht vollständig grün. CSV-Code und diese
Tests wurden nicht verändert.

## Noch ausstehend nach Zusammenführung auf main

Jakobs Wetterimplementierung wurde weder kopiert noch gemergt oder geändert.
Kein echter HTTP-/Wind-Lauf wurde für diesen Schritt ausgeführt. Dieser Nachweis
erfolgt am gemeinsamen Stand: Wetterprozess und Wind starten, positive Erzeugung
prüfen, Prozess stoppen, Stale-Warnung beobachten, Prozess neu starten und frische
Werte prüfen. Startbefehle und Merge-Hinweise stehen im Wind-README.

Beim Zusammenführen müssen Wind in der dynamischen Geräteliste und Wetter-URL
im Hilfetext sowie beide Solution-Projekte erhalten bleiben. Auf dem aktuellen
`jonas` ist `--weather-url` noch nicht implementiert; deshalb wurde kein scheinbar
bereits funktionierendes HTTP-Startprofil ergänzt.
