namespace EnergySim.Pv;

/// <summary>Operating state of the plant.</summary>
public enum PvPlantStatus
{
    Off, // switched off manually
    Standby, // on, no irradiance (night) or no weather
    Running, // on, running free
    Limited, // on, capped by power limit
    Fault // inverter fault
}
