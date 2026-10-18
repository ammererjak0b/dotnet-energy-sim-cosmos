namespace EnergySim.Weather;

/// <summary>The fixed set of locations (same coordinates as tools/fetch-weather.cs).</summary>
public static class LocationCatalog
{
    private const string DISPLAY_TIME_ZONE = "Europe/Vienna";

    private static readonly Dictionary<string, Location> _locations = createLocations();

    private static Dictionary<string, Location> createLocations()
    {
        Dictionary<string, Location> locations = new Dictionary<string, Location>();
        locations["salzburg"] = new Location("salzburg", 47.80, 13.04, DISPLAY_TIME_ZONE);
        locations["wien"] = new Location("wien", 48.21, 16.37, DISPLAY_TIME_ZONE);
        locations["innsbruck"] = new Location("innsbruck", 47.26, 11.39, DISPLAY_TIME_ZONE);
        return locations;
    }

    /// <summary>Returns the location or throws <see cref="UnknownLocationException"/>.</summary>
    public static Location get(string id)
    {
        if (id == null)
        {
            throw new UnknownLocationException(string.Empty);
        }
        if (!_locations.TryGetValue(id, out Location? location))
        {
            throw new UnknownLocationException(id);
        }
        return location;
    }

    public static IReadOnlyList<string> ids()
    {
        return new List<string>(_locations.Keys);
    }
}
