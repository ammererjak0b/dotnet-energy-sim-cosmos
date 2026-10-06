namespace LB_01.src.Domain.Entities;

public class DualPortCharger
{
    public DualPortCharger(
        int id,
        string name,
        double port1MaximumPowerKw,
        double port2MaximumPowerKw,
        double maximumTotalPowerKw)
    {
        this.id = id;
        this.name = name;
        this.maximumTotalPowerKw = maximumTotalPowerKw;
        port1 = new ChargePort
        {
            portNumber = 1,
            maximumPowerKw = port1MaximumPowerKw
        };
        port2 = new ChargePort
        {
            portNumber = 2,
            maximumPowerKw = port2MaximumPowerKw
        };
    }

    public int id { get; }

    public string name { get; }

    public double maximumTotalPowerKw { get; }

    public ChargerStatus status { get; private set; } = ChargerStatus.On;

    public string? fault { get; private set; }

    public ChargePort port1 { get; }

    public ChargePort port2 { get; }

    public void turnOff()
    {
        status = ChargerStatus.Off;
    }

    public void turnOn()
    {
        if (fault is null)
        {
            status = ChargerStatus.On;
        }
    }

    public void setFault(string faultDescription)
    {
        fault = faultDescription;
        status = ChargerStatus.Fault;
    }

    public void clearFault()
    {
        fault = null;
        status = ChargerStatus.On;
    }

    public bool tryConnect(ChargePort port, ECar car)
    {
        if (!ReferenceEquals(port, port1) && !ReferenceEquals(port, port2))
        {
            return false;
        }

        return port.tryConnect(car);
    }

    public bool tryDisconnect(ChargePort port)
    {
        if (!ReferenceEquals(port, port1) && !ReferenceEquals(port, port2))
        {
            return false;
        }

        return port.tryDisconnect();
    }
}
