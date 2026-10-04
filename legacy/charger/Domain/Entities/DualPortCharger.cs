namespace LB_01.Domain.Entities;

public class DualPortCharger
{
    public DualPortCharger(
        int id,
        string name,
        double port1MaximumPowerKw,
        double port2MaximumPowerKw,
        double maximumTotalPowerKw)
    {
        Id = id;
        Name = name;
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

    public int Id { get; }

    public string Name { get; }

    public double MaximumTotalPowerKw { get; }

    public ChargePort Port1 { get; }

    public ChargePort Port2 { get; }

    public bool TryConnect(ChargePort port, ECar car)
    {
        if (!ReferenceEquals(port, Port1) && !ReferenceEquals(port, Port2))
        {
            return false;
        }

        return port.TryConnect(car);
    }
}
