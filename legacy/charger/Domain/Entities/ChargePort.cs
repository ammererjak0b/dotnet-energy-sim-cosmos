namespace LB_01.Domain.Entities;

public class ChargePort
{
    public int PortNumber { get; init; }

    public double MaximumPowerKw { get; init; }

    public ECar? ConnectedCar { get; private set; }

    public bool IsOccupied => ConnectedCar is not null;

    // internal so that TryConnect can only be called from within the same assembly (e.g., by DualPortCharger)
    internal bool TryConnect(ECar car)
    {
        if (IsOccupied || car.ConnectedPort is not null)
        {
            return false;
        }

        ConnectedCar = car;
        car.ConnectedPort = this;
        return true;
    }
}
