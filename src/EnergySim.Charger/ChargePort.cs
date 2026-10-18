namespace EnergySim.Charger;

/// <summary>One plug of the charger: car, fault flag and current power.</summary>
public sealed class ChargePort
{
    public ChargePort(int portNumber, double maxPowerKw)
    {
        PortNumber = portNumber;
        MaxPowerKw = maxPowerKw;
    }

    public int PortNumber { get; }

    public double MaxPowerKw { get; }

    public ECar? Car { get; set; } // null = free

    public bool HasFault { get; set; }

    public double PowerKw { get; set; } // charging power of the last step

    public bool IsOccupied
    {
        get
        {
            return Car != null;
        }
    }
}
