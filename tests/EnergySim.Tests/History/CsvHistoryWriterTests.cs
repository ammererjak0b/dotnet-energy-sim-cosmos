using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.History;
using Microsoft.Extensions.Time.Testing;

namespace EnergySim.Tests.History;

public class CsvHistoryWriterTests : IDisposable
{
    private static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);
    private static readonly DateTimeOffset START = new DateTimeOffset(2026, 6, 21, 11, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), "energysim-history-" + Guid.NewGuid().ToString("N") + ".csv");
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider();

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private static SimulationFrame createFrame(DateTimeOffset virtualNow, int powerW)
    {
        SimulationFrame frame = new SimulationFrame();
        frame.VirtualNow = virtualNow;
        frame.Header.State = DeviceState.Limited;
        frame.Header.ActivePowerW = powerW;
        frame.Header.EnergyWh = 1234;
        frame.Header.PowerLimitPercent = 12.5;
        frame.Header.FaultCode = 2;
        frame.DisplayRows = new List<DisplayRow> { DeviceSupport.createRow("SoC", "55.5 %"), DeviceSupport.createRow("Mode", "a;b") };
        return frame;
    }

    private async Task<string[]> readLinesAsync()
    {
        using FileStream stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        using StreamReader reader = new StreamReader(stream);
        List<string> lines = new List<string>();
        string? line = await reader.ReadLineAsync();
        while (line != null)
        {
            lines.Add(line);
            line = await reader.ReadLineAsync();
        }

        return lines.ToArray();
    }

    [Fact]
    public async Task PublishAsync_FirstFrame_WritesHeaderFromFrameAndOneRow()
    {
        // Arrange
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);

        // Act
        await writer.publishAsync(createFrame(START, -5000), CancellationToken.None);

        // Assert
        string[] lines = await readLinesAsync();
        Assert.Equal(2, lines.Length);
        Assert.Equal("virtualTimeUtc;state;activePowerW;energyWh;limitPercent;faultCode;SoC;Mode", lines[0]);
        Assert.Equal("2026-06-21T11:00:00.0000000Z;Limited;-5000;1234;12.5;2;55.5 %;\"a;b\"", lines[1]);
    }

    [Fact]
    public async Task PublishAsync_WithinInterval_WritesNoSecondRow()
    {
        // Arrange
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);
        await writer.publishAsync(createFrame(START, 1), CancellationToken.None);

        // Act
        _timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        await writer.publishAsync(createFrame(START.AddMinutes(1), 2), CancellationToken.None);

        // Assert
        Assert.Equal(2, (await readLinesAsync()).Length);
    }

    [Fact]
    public async Task PublishAsync_AfterInterval_WritesNextRow()
    {
        // Arrange
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);
        await writer.publishAsync(createFrame(START, 1), CancellationToken.None);

        // Act
        _timeProvider.Advance(INTERVAL);
        await writer.publishAsync(createFrame(START.AddMinutes(1), 2), CancellationToken.None);

        // Assert
        string[] lines = await readLinesAsync();
        Assert.Equal(3, lines.Length);
        Assert.StartsWith("2026-06-21T11:01:00", lines[2]);
    }

    [Fact]
    public async Task PublishAsync_VirtualTimeUnchanged_WritesNoRow()
    {
        // Arrange: clock paused => same virtual time
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);
        await writer.publishAsync(createFrame(START, 1), CancellationToken.None);

        // Act
        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        await writer.publishAsync(createFrame(START, 1), CancellationToken.None);

        // Assert
        Assert.Equal(2, (await readLinesAsync()).Length);
    }

    [Fact]
    public async Task PublishAsync_ExistingFile_IsReplaced()
    {
        // Arrange
        await File.WriteAllTextAsync(_path, "old content\n");
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);

        // Act
        await writer.publishAsync(createFrame(START, 1), CancellationToken.None);

        // Assert
        Assert.DoesNotContain("old content", string.Join("\n", await readLinesAsync()));
    }

    [Fact]
    public async Task PublishAsync_ForeignCulture_StillWritesInvariantNumbers()
    {
        // Arrange
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider, INTERVAL);

            // Act
            await writer.publishAsync(createFrame(START, 1), CancellationToken.None);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }

        // Assert
        Assert.Contains(";12.5;", (await readLinesAsync())[1]);
    }

    [Fact]
    public async Task PublishAsync_PerFrameMode_WritesEachNewSequenceWithoutWaitingForRealTime()
    {
        // Arrange
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider);
        SimulationFrame first = createFrame(START, 1);
        first.Sequence = 1;
        first.CsvHistoryFields = new List<CsvHistoryField> { new CsvHistoryField("legacy", "one") };
        SimulationFrame second = createFrame(START.AddHours(1), 2);
        second.Sequence = 2;
        second.CsvHistoryFields = new List<CsvHistoryField> { new CsvHistoryField("legacy", "two") };

        // Act
        await writer.publishAsync(first, CancellationToken.None);
        await writer.publishAsync(second, CancellationToken.None);

        // Assert
        string[] lines = await readLinesAsync();
        Assert.Equal(3, lines.Length);
        Assert.Equal("legacy", lines[0]);
        Assert.Equal("one", lines[1]);
        Assert.Equal("two", lines[2]);
    }

    [Fact]
    public async Task PublishAsync_PerFrameMode_SkipsDuplicateSequence()
    {
        // Arrange
        await using CsvHistoryWriter writer = new CsvHistoryWriter(_path, _timeProvider);
        SimulationFrame frame = createFrame(START, 1);
        frame.Sequence = 4;

        // Act
        await writer.publishAsync(frame, CancellationToken.None);
        await writer.publishAsync(frame, CancellationToken.None);

        // Assert
        Assert.Equal(2, (await readLinesAsync()).Length);
    }

    [Fact]
    public void Constructor_NonPositiveInterval_Throws()
    {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(delegate { new CsvHistoryWriter(_path, _timeProvider, TimeSpan.Zero); });
    }
}
