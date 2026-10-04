using LB_01.Domain.Entities;

namespace LB_01.Tests;

public class ChargePortConnectionTests
{
    private const int ChargerId = 1;
    private const string ChargerName = "charger-1";
    private const double PortMaximumPowerKw = 11;
    private const double MaximumTotalPowerKw = 22;
    private const int FirstCarId = 1;
    private const int SecondCarId = 2;

    [Fact]
    public void ConnectsCarToFreePort()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar car = new() { Id = FirstCarId };

        bool connected = charger.TryConnect(charger.Port1, car);

        Assert.True(connected);
        Assert.True(charger.Port1.IsOccupied);
        Assert.Same(car, charger.Port1.ConnectedCar);
    }

    [Fact]
    public void DisconnectsCarFromPort()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar car = new() { Id = FirstCarId };
        charger.TryConnect(charger.Port1, car);

        bool disconnected = charger.TryDisconnect(charger.Port1);

        Assert.True(disconnected);
        Assert.False(charger.Port1.IsOccupied);
        Assert.Null(charger.Port1.ConnectedCar);
        Assert.Null(car.ConnectedPort);
    }

    [Fact]
    public void DisconnectingOnePortDoesNotAffectTheOtherPort()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar firstCar = new() { Id = FirstCarId };
        ECar secondCar = new() { Id = SecondCarId };
        charger.TryConnect(charger.Port1, firstCar);
        charger.TryConnect(charger.Port2, secondCar);

        bool disconnected = charger.TryDisconnect(charger.Port1);

        Assert.True(disconnected);
        Assert.False(charger.Port1.IsOccupied);
        Assert.Null(firstCar.ConnectedPort);
        Assert.True(charger.Port2.IsOccupied);
        Assert.Same(secondCar, charger.Port2.ConnectedCar);
        Assert.Same(charger.Port2, secondCar.ConnectedPort);
    }

    [Fact]
    public void RejectsDisconnectWhenPortIsFree()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);

        bool disconnected = charger.TryDisconnect(charger.Port1);

        Assert.False(disconnected);
        Assert.False(charger.Port1.IsOccupied);
    }

    [Fact]
    public void RejectsCarWhenPortIsAlreadyOccupied()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar firstCar = new() { Id = FirstCarId };
        ECar secondCar = new() { Id = SecondCarId };
        charger.TryConnect(charger.Port1, firstCar);

        bool connected = charger.TryConnect(charger.Port1, secondCar);

        Assert.False(connected);
        Assert.Same(firstCar, charger.Port1.ConnectedCar);
    }

    [Fact]
    public void BothPortsCanConnectDifferentCarsIndependently()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar firstCar = new() { Id = FirstCarId };
        ECar secondCar = new() { Id = SecondCarId };

        bool firstConnection = charger.TryConnect(charger.Port1, firstCar);
        bool secondConnection = charger.TryConnect(charger.Port2, secondCar);

        Assert.True(firstConnection);
        Assert.True(secondConnection);
        Assert.Same(firstCar, charger.Port1.ConnectedCar);
        Assert.Same(secondCar, charger.Port2.ConnectedCar);
    }

    [Fact]
    public void RejectsCarWhenItIsAlreadyConnectedToAnotherPort()
    {
        DualPortCharger charger = new(
            ChargerId,
            ChargerName,
            PortMaximumPowerKw,
            PortMaximumPowerKw,
            MaximumTotalPowerKw);
        ECar car = new() { Id = FirstCarId };

        bool firstConnection = charger.TryConnect(charger.Port1, car);
        bool secondConnection = charger.TryConnect(charger.Port2, car);

        Assert.True(firstConnection);
        Assert.False(secondConnection);
        Assert.Same(car, charger.Port1.ConnectedCar);
        Assert.False(charger.Port2.IsOccupied);
    }
}
