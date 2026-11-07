using System.Globalization;
using System.Text;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;

namespace EnergySim.Core.History;

/// <summary>
/// Writes the device history as CSV (semicolon, invariant culture, one file per run, replaced if it exists).
/// One row per interval of REAL time, and only when the virtual time moved (no rows while paused).
/// Header comes from the first frame: virtualTimeUtc;state;activePowerW;energyWh;limitPercent;faultCode + one column per DisplayRow.
/// Display columns are matched by position, so a changing row label (e.g. car name) does not break the file.
/// </summary>
public sealed class CsvHistoryWriter : ISimulationSink, IAsyncDisposable
{
    public static readonly TimeSpan DEFAULT_INTERVAL = TimeSpan.FromSeconds(1);

    private const char SEPARATOR = ';';

    private readonly string _path;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _interval;
    private readonly bool _writeEveryFrame;
    private StreamWriter? _writer;
    private int _displayColumnCount;
    private long _lastRowTimestamp;
    private DateTimeOffset _lastRowVirtualTime;
    private uint _lastRowSequence;

    public CsvHistoryWriter(string path, TimeProvider timeProvider, TimeSpan interval)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("path must not be empty.", nameof(path));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "interval must be positive.");
        }

        _path = path;
        _timeProvider = timeProvider;
        _interval = interval;
    }

    public CsvHistoryWriter(string path, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("path must not be empty.", nameof(path));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _path = path;
        _timeProvider = timeProvider;
        _interval = TimeSpan.Zero;
        _writeEveryFrame = true;
    }

    public async Task publishAsync(SimulationFrame frame, CancellationToken ct)
    {
        if (_writer == null)
        {
            _writer = openFile();
            _displayColumnCount = frame.CsvHistoryFields.Count == 0 ? frame.DisplayRows.Count : 0;
            await _writer.WriteLineAsync(buildHeader(frame).AsMemory(), ct);
        }
        else if (!isRowDue(frame))
        {
            return;
        }

        await _writer.WriteLineAsync(buildRow(frame).AsMemory(), ct);
        await _writer.FlushAsync(ct); // file readable while the run is going
        _lastRowTimestamp = _timeProvider.GetTimestamp();
        _lastRowVirtualTime = frame.VirtualNow;
        _lastRowSequence = frame.Sequence;
    }

    public async ValueTask DisposeAsync()
    {
        if (_writer != null)
        {
            await _writer.DisposeAsync();
            _writer = null;
        }
    }

    private StreamWriter openFile()
    {
        FileStream stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
        return new StreamWriter(stream, new UTF8Encoding(false));
    }

    private bool isRowDue(SimulationFrame frame)
    {
        if (_writeEveryFrame)
        {
            return frame.Sequence != _lastRowSequence;
        }

        if (frame.VirtualNow == _lastRowVirtualTime)
        {
            return false; // paused or not advanced
        }

        return _timeProvider.GetElapsedTime(_lastRowTimestamp) >= _interval;
    }

    private string buildHeader(SimulationFrame frame)
    {
        if (frame.CsvHistoryFields.Count > 0)
        {
            List<string> historyColumns = new List<string>();
            foreach (CsvHistoryField field in frame.CsvHistoryFields)
            {
                historyColumns.Add(escape(field.Name));
            }

            return string.Join(',', historyColumns);
        }

        List<string> columns = new List<string> { "virtualTimeUtc", "state", "activePowerW", "energyWh", "limitPercent", "faultCode" };
        foreach (DisplayRow row in frame.DisplayRows)
        {
            columns.Add(escape(row.Label));
        }

        return string.Join(SEPARATOR, columns);
    }

    private string buildRow(SimulationFrame frame)
    {
        List<string> columns = new List<string>();
        if (frame.CsvHistoryFields.Count > 0)
        {
            foreach (CsvHistoryField field in frame.CsvHistoryFields)
            {
                columns.Add(escapeCommaCsv(field.Value));
            }

            return string.Join(',', columns);
        }

        columns.Add(frame.VirtualNow.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
        columns.Add(frame.Header.State.ToString());
        columns.Add(frame.Header.ActivePowerW.ToString(CultureInfo.InvariantCulture));
        columns.Add(frame.Header.EnergyWh.ToString(CultureInfo.InvariantCulture));
        columns.Add(frame.Header.PowerLimitPercent.ToString("0.##", CultureInfo.InvariantCulture));
        columns.Add(frame.Header.FaultCode.ToString(CultureInfo.InvariantCulture));
        for (int index = 0; index < _displayColumnCount; index++)
        {
            string value = string.Empty;
            if (index < frame.DisplayRows.Count)
            {
                value = frame.DisplayRows[index].Value;
            }

            columns.Add(escape(value));
        }

        return string.Join(SEPARATOR, columns);
    }

    private static string escape(string text)
    {
        if (text.IndexOfAny(new char[] { SEPARATOR, '"', '\n' }) < 0)
        {
            return text;
        }

        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static string escapeCommaCsv(string text)
    {
        if (text.IndexOfAny(new char[] { ',', '"', '\n', '\r' }) < 0)
        {
            return text;
        }

        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
