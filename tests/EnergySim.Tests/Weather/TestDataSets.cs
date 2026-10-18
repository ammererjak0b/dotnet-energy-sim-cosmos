using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

/// <summary>Small in-memory data sets for unit tests (value depends on the hour index).</summary>
internal static class TestDataSets
{
    public const string LOCATION_ID = "salzburg";

    /// <summary>Ramp data: temperature = hour index, wind direction = 10 deg per hour.</summary>
    public static WeatherDataSet createRamp()
    {
        int hours = WeatherDataSet.HOURS_PER_YEAR;
        double[] temperature = new double[hours];
        double[] cloud = new double[hours];
        double[] radiation = new double[hours];
        double[] precipitation = new double[hours];
        double[] windSpeed = new double[hours];
        double[] windDirection = new double[hours];
        for (int index = 0; index < hours; index++)
        {
            temperature[index] = index;
            cloud[index] = index % 101;
            radiation[index] = index % 1000;
            precipitation[index] = index % 7;
            windSpeed[index] = index % 10;
            windDirection[index] = (index * 10) % 360;
        }
        return new WeatherDataSet(temperature, cloud, radiation, precipitation, windSpeed, windDirection);
    }

    public static WeatherModel createModel(WeatherDataSet dataSet)
    {
        Dictionary<string, WeatherDataSet> dataSets = new Dictionary<string, WeatherDataSet>();
        dataSets[LOCATION_ID] = dataSet;
        return new WeatherModel(dataSets);
    }

    /// <summary>Hour index in the 2025 reference year (non leap).</summary>
    public static int hourIndex(int month, int day, int hour)
    {
        return (new DateTime(2025, month, day).DayOfYear - 1) * 24 + hour;
    }
}
