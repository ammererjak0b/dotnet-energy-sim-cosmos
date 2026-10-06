using LB_01.src.Domain.Entities;

namespace LB_01.Tests;

public class ChargePortConnectionTests
{
    private const int CHARGER_ID = 1;
    private const string CHARGER_NAME = "charger-1";
    private const double PORT_MAXIMUM_POWER_KW = 11;
    private const double MAXIMUM_TOTAL_POWER_KW = 22;
    private const int FIRST_CAR_ID = 1;
    private const int SECOND_CAR_ID = 2;

    [Fact]
    public void ConnectsCarToFreePort()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new() { id = FIRST_CAR_ID };

        bool connected = charger.tryConnect(charger.port1, car);

        Assert.True(connected);
        Assert.True(charger.port1.isOccupied);
        Assert.Same(car, charger.port1.connectedCar);
    }

    [Fact]
    public void DisconnectsCarFromPort()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new() { id = FIRST_CAR_ID };
        charger.tryConnect(charger.port1, car);

        bool disconnected = charger.tryDisconnect(charger.port1);

        Assert.True(disconnected);
        Assert.False(charger.port1.isOccupied);
        Assert.Null(charger.port1.connectedCar);
        Assert.Null(car.connectedPort);
    }

    [Fact]
    public void DisconnectingOnePortDoesNotAffectTheOtherPort()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar firstCar = new() { id = FIRST_CAR_ID };
        ECar secondCar = new() { id = SECOND_CAR_ID };
        charger.tryConnect(charger.port1, firstCar);
        charger.tryConnect(charger.port2, secondCar);

        bool disconnected = charger.tryDisconnect(charger.port1);

        Assert.True(disconnected);
        Assert.False(charger.port1.isOccupied);
        Assert.Null(firstCar.connectedPort);
        Assert.True(charger.port2.isOccupied);
        Assert.Same(secondCar, charger.port2.connectedCar);
        Assert.Same(charger.port2, secondCar.connectedPort);
    }

    [Fact]
    public void RejectsDisconnectWhenPortIsFree()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);

        bool disconnected = charger.tryDisconnect(charger.port1);

        Assert.False(disconnected);
        Assert.False(charger.port1.isOccupied);
    }

    [Fact]
    public void RejectsCarWhenPortIsAlreadyOccupied()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar firstCar = new() { id = FIRST_CAR_ID };
        ECar secondCar = new() { id = SECOND_CAR_ID };
        charger.tryConnect(charger.port1, firstCar);

        bool connected = charger.tryConnect(charger.port1, secondCar);

        Assert.False(connected);
        Assert.Same(firstCar, charger.port1.connectedCar);
    }

    [Fact]
    public void BothPortsCanConnectDifferentCarsIndependently()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar firstCar = new() { id = FIRST_CAR_ID };
        ECar secondCar = new() { id = SECOND_CAR_ID };

        bool firstConnection = charger.tryConnect(charger.port1, firstCar);
        bool secondConnection = charger.tryConnect(charger.port2, secondCar);

        Assert.True(firstConnection);
        Assert.True(secondConnection);
        Assert.Same(firstCar, charger.port1.connectedCar);
        Assert.Same(secondCar, charger.port2.connectedCar);
    }

    [Fact]
    public void RejectsCarWhenItIsAlreadyConnectedToAnotherPort()
    {
        DualPortCharger charger = new(
            CHARGER_ID,
            CHARGER_NAME,
            PORT_MAXIMUM_POWER_KW,
            PORT_MAXIMUM_POWER_KW,
            MAXIMUM_TOTAL_POWER_KW);
        ECar car = new() { id = FIRST_CAR_ID };

        bool firstConnection = charger.tryConnect(charger.port1, car);
        bool secondConnection = charger.tryConnect(charger.port2, car);

        Assert.True(firstConnection);
        Assert.False(secondConnection);
        Assert.Same(car, charger.port1.connectedCar);
        Assert.False(charger.port2.isOccupied);
    }
}
