namespace EnergySim.Weather;

/// <summary>Location id is not in the catalog (HTTP 404 in the weather exe).</summary>
public sealed class UnknownLocationException : Exception
{
    public UnknownLocationException(string locationId)
        : base("Unknown location id '" + locationId + "'")
    {
        LocationId = locationId;
    }

    public string LocationId { get; }
}
