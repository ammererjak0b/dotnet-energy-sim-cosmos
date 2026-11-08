namespace EnergySim.Messaging;

/// <summary>Routing keys and bindings of the telemetry exchange.</summary>
public static class Routing
{
    public const string DEFAULT_EXCHANGE = "energysim.telemetry";
    public const string BIND_ALL = "telemetry.#";

    /// <summary>telemetry.pv.pv-1</summary>
    public static string routingKey(string deviceType, string deviceId)
    {
        return "telemetry." + deviceType.ToLowerInvariant() + "." + deviceId;
    }
}
