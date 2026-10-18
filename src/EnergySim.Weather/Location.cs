namespace EnergySim.Weather;

/// <summary>One weather location. Time zone is display only, all times in the API are UTC.</summary>
public sealed class Location
{
    public Location(string id, double latitude, double longitude, string timeZoneDisplayName)
    {
        Id = id;
        Latitude = latitude;
        Longitude = longitude;
        TimeZoneDisplayName = timeZoneDisplayName;
    }

    public string Id { get; }

    public double Latitude { get; } // deg north

    public double Longitude { get; } // deg east

    public string TimeZoneDisplayName { get; }
}
