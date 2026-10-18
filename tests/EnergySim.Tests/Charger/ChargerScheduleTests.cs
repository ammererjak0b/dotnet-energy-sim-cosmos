using EnergySim.Core.Devices;
using EnergySim.Charger;

namespace EnergySim.Tests.Charger;

public class ChargerScheduleTests
{
    private static ChargerDevice createScheduledDevice(CarScheduleEntry entry)
    {
        ChargerOptions options = ChargerTestSupport.createOptions();
        options.Schedule.Add(entry);
        return ChargerTestSupport.createDevice(options);
    }

    private static void tick(ChargerDevice device, int hour, int minute, TimeSpan step)
    {
        device.update(ChargerTestSupport.utcTime(hour, minute), step, null);
    }

    [Fact]
    public void Update_BeforeArrival_NoCarConnected()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));

        // Act
        tick(device, 5, 59, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_AtArrivalTime_ConnectsScheduledCarWithStartSoc()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));

        // Act
        tick(device, 6, 0, TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(30.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 0);
        Assert.Equal(-11000, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_ScheduledCarOnPort2_UsesPort2()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(2, 7, 30, 9, 0));

        // Act
        tick(device, 7, 30, TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
        Assert.True(ChargerTestSupport.getValue(device, "Port2SocPercent") > 0);
    }

    [Fact]
    public void Update_AtDepartureTime_DisconnectsCar()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        tick(device, 9, 59, TimeSpan.FromMinutes(1));

        // Act
        tick(device, 10, 0, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_TickJumpsOverArrival_CarStillArrives()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        tick(device, 5, 0, TimeSpan.FromMinutes(1));

        // Act
        tick(device, 8, 0, TimeSpan.FromMinutes(1));

        // Assert
        Assert.True(ChargerTestSupport.getValue(device, "Port1SocPercent") > 0);
    }

    [Fact]
    public void Update_TickJumpsOverDeparture_CarStillLeaves()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        tick(device, 8, 0, TimeSpan.FromMinutes(1));

        // Act
        tick(device, 13, 0, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
    }

    [Fact]
    public void Update_NextDay_CarArrivesAgainWithStartSoc()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 8, 0));
        tick(device, 6, 0, TimeSpan.FromHours(1));
        tick(device, 7, 0, TimeSpan.FromHours(1));
        tick(device, 8, 0, TimeSpan.FromMinutes(1));

        // Act
        device.update(ChargerTestSupport.utcTime(6, 0).AddDays(1), TimeSpan.FromSeconds(1), null);

        // Assert
        Assert.Equal(30.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 0);
    }

    [Fact]
    public void Update_WindowAcrossMidnight_CarStaysOverNight()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 22, 0, 6, 0));

        // Act
        tick(device, 23, 0, TimeSpan.FromMinutes(1));
        double socAtNight = ChargerTestSupport.getValue(device, "Port1SocPercent");
        device.update(ChargerTestSupport.utcTime(3, 0).AddDays(1), TimeSpan.FromMinutes(1), null);
        double socAtMorning = ChargerTestSupport.getValue(device, "Port1SocPercent");
        device.update(ChargerTestSupport.utcTime(6, 0).AddDays(1), TimeSpan.FromMinutes(1), null);

        // Assert
        Assert.True(socAtNight > 0);
        Assert.True(socAtMorning > socAtNight);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
    }

    [Fact]
    public void Update_PortOccupied_ScheduledCarWaitsUntilPortIsFree()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        device.applyCommand(new DeviceCommand("ConnectCar", 1));
        tick(device, 6, 0, TimeSpan.FromMinutes(1));
        double socWithManualCar = ChargerTestSupport.getValue(device, "Port1SocPercent");
        device.applyCommand(new DeviceCommand("DisconnectCar", 1));

        // Act
        tick(device, 6, 1, TimeSpan.FromMinutes(1));

        // Assert
        Assert.True(socWithManualCar > 59);
        Assert.InRange(ChargerTestSupport.getValue(device, "Port1SocPercent"), 29.0, 31.0);
    }

    [Fact]
    public void Update_ScheduledCarDisconnectedManually_DoesNotReturnInSameWindow()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        tick(device, 6, 0, TimeSpan.FromMinutes(1));
        device.applyCommand(new DeviceCommand("DisconnectCar", 1));

        // Act
        tick(device, 7, 0, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
    }

    [Fact]
    public void Update_ScheduledCarIsFullyDeterministic_SameInputsSameResult()
    {
        // Arrange
        ChargerDevice first = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));
        ChargerDevice second = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));

        // Act
        tick(first, 6, 0, TimeSpan.FromMinutes(30));
        tick(second, 6, 0, TimeSpan.FromMinutes(30));

        // Assert
        Assert.Equal(first.Header.EnergyWh, second.Header.EnergyWh);
        Assert.Equal(first.Header.ActivePowerW, second.Header.ActivePowerW);
    }

    [Fact]
    public void Constructor_DefaultOptions_HaveActivityInTheMorning()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice(new ChargerOptions());

        // Act
        tick(device, 6, 0, TimeSpan.FromMinutes(1));
        bool port1Busy = ChargerTestSupport.getValue(device, "Port1SocPercent") > 0;
        tick(device, 7, 30, TimeSpan.FromMinutes(1));
        bool port2Busy = ChargerTestSupport.getValue(device, "Port2SocPercent") > 0;

        // Assert
        Assert.True(port1Busy);
        Assert.True(port2Busy);
    }

    [Theory]
    [InlineData(0, 6, 0, 10, 0)]
    [InlineData(3, 6, 0, 10, 0)]
    [InlineData(1, 24, 0, 10, 0)]
    [InlineData(1, 6, 60, 10, 0)]
    [InlineData(1, 6, 0, -1, 0)]
    [InlineData(1, 6, 0, 10, 60)]
    public void Constructor_InvalidScheduleEntry_Throws(int port, int arriveHour, int arriveMinute, int departHour, int departMinute)
    {
        // Arrange
        ChargerOptions options = ChargerTestSupport.createOptions();
        options.Schedule.Add(ChargerTestSupport.createEntry(port, arriveHour, arriveMinute, departHour, departMinute));

        // Act
        Action action = delegate
        {
            ChargerTestSupport.createDevice(options);
        };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));
        device.applyCommand(new DeviceCommand("FaultPort", 2));
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 40));
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));
        ChargerTestSupport.advance(device, TimeSpan.FromHours(1));

        // Act
        device.reset(ChargerTestSupport.MidnightUtc);

        // Assert
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.True(device.Header.IsOn);
        Assert.Equal(0, device.Header.FaultCode);
        Assert.Equal(100.0, device.Header.PowerLimitPercent, 6);
        Assert.Equal(0L, device.Header.EnergyWh);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2Status"), 6);
    }

    [Fact]
    public void Reset_EvaluatesScheduleForGivenTime()
    {
        // Arrange
        ChargerDevice device = createScheduledDevice(ChargerTestSupport.createEntry(1, 6, 0, 10, 0));

        // Act
        device.reset(ChargerTestSupport.utcTime(8, 0));

        // Assert
        Assert.Equal(30.0, ChargerTestSupport.getValue(device, "Port1SocPercent"), 6);
    }
}
