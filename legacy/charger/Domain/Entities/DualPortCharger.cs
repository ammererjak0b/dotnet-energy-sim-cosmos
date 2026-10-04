namespace LB_01.Domain.Entities;

public class DualPortCharger
{
    public DualPortCharger(
        string id,
        double port1MaximumPowerKw,
        double port2MaximumPowerKw,
        double maximumTotalPowerKw)
    {
        Id = id;
        MaximumTotalPowerKw = maximumTotalPowerKw;
        Port1 = new ChargePort
        {
            PortNumber = 1,
            MaximumPowerKw = port1MaximumPowerKw
        };
        Port2 = new ChargePort
        {
            PortNumber = 2,
            MaximumPowerKw = port2MaximumPowerKw
        };
    }

    public string Id { get; }

    public double MaximumTotalPowerKw { get; }

    public ChargePort Port1 { get; }

    public ChargePort Port2 { get; }
}
