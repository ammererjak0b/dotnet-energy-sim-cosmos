namespace LB_01.Domain.Entities;

public class ChargePort
{
    public int PortNumber { get; init; }

    public double MaximumPowerKw { get; init; }

    public ECar? ConnectedCar { get; set; }

    public bool IsOccupied => ConnectedCar is not null;
}
