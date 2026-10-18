using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnergySim.Weather;

/// <summary>Loads Open-Meteo archive JSON files (hourly arrays) async. Wind speed is converted to m/s.</summary>
public static class WeatherDataLoader
{
    public const int REFERENCE_YEAR = 2025;

    private const double KMH_PER_MS = 3.6;

    private sealed class ArchiveFile
    {
        [JsonPropertyName("hourly_units")]
        public ArchiveUnits? HourlyUnits { get; set; }

        [JsonPropertyName("hourly")]
        public ArchiveHourly? Hourly { get; set; }
    }

    private sealed class ArchiveUnits
    {
        [JsonPropertyName("wind_speed_10m")]
        public string? WindSpeed10m { get; set; }
    }

    private sealed class ArchiveHourly
    {
        [JsonPropertyName("time")]
        public List<string>? Time { get; set; }

        [JsonPropertyName("temperature_2m")]
        public List<double?>? Temperature2m { get; set; }

        [JsonPropertyName("cloud_cover")]
        public List<double?>? CloudCover { get; set; }

        [JsonPropertyName("shortwave_radiation")]
        public List<double?>? ShortwaveRadiation { get; set; }

        [JsonPropertyName("precipitation")]
        public List<double?>? Precipitation { get; set; }

        [JsonPropertyName("wind_speed_10m")]
        public List<double?>? WindSpeed10m { get; set; }

        [JsonPropertyName("wind_direction_10m")]
        public List<double?>? WindDirection10m { get; set; }
    }

    /// <summary>Loads <c>&lt;id&gt;-2025.json</c> of every catalog location from the directory.</summary>
    public static async Task<IReadOnlyDictionary<string, WeatherDataSet>> loadDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        Dictionary<string, WeatherDataSet> dataSets = new Dictionary<string, WeatherDataSet>();
        foreach (string id in LocationCatalog.ids())
        {
            string path = Path.Combine(directory, id + "-" + REFERENCE_YEAR + ".json");
            dataSets[id] = await loadFileAsync(path, cancellationToken);
        }
        return dataSets;
    }

    public static async Task<WeatherDataSet> loadFileAsync(string path, CancellationToken cancellationToken)
    {
        ArchiveFile? file;
        try
        {
            await using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            file = await JsonSerializer.DeserializeAsync<ArchiveFile>(stream, cancellationToken: cancellationToken);
        }
        catch (FileNotFoundException exception)
        {
            throw new WeatherDataException("Weather file not found: " + path, exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            throw new WeatherDataException("Weather directory not found: " + path, exception);
        }
        catch (JsonException exception)
        {
            throw new WeatherDataException("Weather file is not valid Open-Meteo JSON: " + path, exception);
        }

        if (file == null || file.Hourly == null)
        {
            throw new WeatherDataException("Weather file has no 'hourly' section: " + path);
        }
        double windFactor = readWindFactor(file, path);
        ArchiveHourly hourly = file.Hourly;
        checkRowCount(hourly.Time, "time", path);
        double[] windSpeedMs = toArray(hourly.WindSpeed10m, "wind_speed_10m", path);
        for (int index = 0; index < windSpeedMs.Length; index++)
        {
            windSpeedMs[index] = windSpeedMs[index] * windFactor;
        }
        return new WeatherDataSet(
            toArray(hourly.Temperature2m, "temperature_2m", path),
            toArray(hourly.CloudCover, "cloud_cover", path),
            toArray(hourly.ShortwaveRadiation, "shortwave_radiation", path),
            toArray(hourly.Precipitation, "precipitation", path),
            windSpeedMs,
            toArray(hourly.WindDirection10m, "wind_direction_10m", path));
    }

    // factor to m/s from the unit stated in the file
    private static double readWindFactor(ArchiveFile file, string path)
    {
        if (file.HourlyUnits == null || file.HourlyUnits.WindSpeed10m == null)
        {
            return 1.0; // no unit given: request used wind_speed_unit=ms
        }
        string unit = file.HourlyUnits.WindSpeed10m;
        if (unit == "m/s")
        {
            return 1.0;
        }
        if (unit == "km/h")
        {
            return 1.0 / KMH_PER_MS;
        }
        throw new WeatherDataException("Unsupported wind speed unit '" + unit + "' in " + path);
    }

    private static void checkRowCount(List<string>? times, string fieldName, string path)
    {
        if (times == null || times.Count != WeatherDataSet.HOURS_PER_YEAR)
        {
            int actual = 0;
            if (times != null)
            {
                actual = times.Count;
            }
            throw new WeatherDataException("Field '" + fieldName + "' has " + actual + " hourly rows, expected " + WeatherDataSet.HOURS_PER_YEAR + " in " + path);
        }
    }

    private static double[] toArray(List<double?>? values, string fieldName, string path)
    {
        if (values == null || values.Count != WeatherDataSet.HOURS_PER_YEAR)
        {
            int actual = 0;
            if (values != null)
            {
                actual = values.Count;
            }
            throw new WeatherDataException("Field '" + fieldName + "' has " + actual + " hourly rows, expected " + WeatherDataSet.HOURS_PER_YEAR + " in " + path);
        }
        double[] result = new double[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            double? value = values[index];
            if (value == null)
            {
                throw new WeatherDataException("Field '" + fieldName + "' has null at row " + index + " in " + path);
            }
            result[index] = value.Value;
        }
        return result;
    }
}
