using EnergySim.Core.Devices;
using EnergySim.Charger;

namespace EnergySim.Tests.Charger;

public class ChargerPhysicsTests
{
    private const double TOLERANCE = 0.01;

    [Fact]
    public void Update_OneCarForOneHour_ChargesAtPortLimit()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(50);
        device.connectCar(1, car);

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(11000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 1);
        Assert.Equal(11000L, device.Header.EnergyWh);
        Assert.Equal(68.333, car.StateOfChargePercent, 2);
    }

    [Fact]
    public void Update_SocAtTaperStart_ChargesWithFullPower()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(80));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(11000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 1);
    }

    [Fact]
    public void Update_SocAboveTaperStart_ChargesWithReducedPower()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(90));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(5500.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
    }

    [Fact]
    public void Update_TaperBelowMinimum_UsesMinimumTaperPower()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(99.5));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(1000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
    }

    [Fact]
    public void Update_CarFromSeventyToFull_StopsAtHundredPercent()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(70);
        device.connectCar(1, car);
        TimeSpan step = TimeSpan.FromMinutes(15);
        DateTimeOffset now = ChargerTestSupport.MidnightUtc;
        int stepCount = 0;

        // Act
        while (car.StateOfChargePercent < 100 && stepCount < 200)
        {
            now = now.Add(step);
            device.update(now, step, null);
            stepCount++;
        }

        // Assert
        Assert.True(stepCount < 200);
        Assert.Equal(100.0, car.StateOfChargePercent, 2);
        Assert.Equal(18000.0, device.Header.EnergyWh, 0);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Standby, device.Header.State);
    }

    [Fact]
    public void Update_FullCar_DrawsNoPower()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(100));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(0L, device.Header.EnergyWh);
    }

    [Fact]
    public void Update_TwoCarsWithTotalLimit_SplitPowerFairly()
    {
        // Arrange
        ChargerOptions options = ChargerTestSupport.createOptions();
        options.TotalMaxPowerKw = 11;
        ChargerDevice device = ChargerTestSupport.createDevice(options);
        device.connectCar(1, ChargerTestSupport.createCar(60, 15));
        device.connectCar(2, ChargerTestSupport.createCar(60, 15));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(5500.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
        Assert.Equal(5500.0, ChargerTestSupport.getValue(device, "Port2PowerW"), 0);
        Assert.Equal(-11000, device.Header.ActivePowerW);
        Assert.Equal(2750.0, device.Header.EnergyWh, 0);
    }

    [Fact]
    public void Update_DifferentPortLimits_RemainingPowerGoesToOtherPort()
    {
        // Arrange
        ChargerOptions options = ChargerTestSupport.createOptions();
        options.Port1MaxPowerKw = 15;
        options.Port2MaxPowerKw = 7;
        options.TotalMaxPowerKw = 22;
        ChargerDevice device = ChargerTestSupport.createDevice(options);
        device.connectCar(1, ChargerTestSupport.createCar(60, 15));
        device.connectCar(2, ChargerTestSupport.createCar(60, 15));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(15000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
        Assert.Equal(7000.0, ChargerTestSupport.getValue(device, "Port2PowerW"), 0);
        Assert.Equal(-22000, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_TaperedCar_LeavesUnusedPowerToOtherCar()
    {
        // Arrange
        ChargerOptions options = ChargerTestSupport.createOptions();
        options.TotalMaxPowerKw = 11;
        ChargerDevice device = ChargerTestSupport.createDevice(options);
        device.connectCar(1, ChargerTestSupport.createCar(90, 15));
        device.connectCar(2, ChargerTestSupport.createCar(60, 15));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(5500.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
        Assert.Equal(5500.0, ChargerTestSupport.getValue(device, "Port2PowerW"), 0);
    }

    [Fact]
    public void Update_CarLimitBelowPortLimit_UsesCarLimit()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50, 7));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(7000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
    }

    [Fact]
    public void Update_Charging_ActivePowerIsNegative()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(-11000, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_SeveralSteps_EnergyAccumulatesAndStaysNonNegative()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));
        DateTimeOffset now = ChargerTestSupport.MidnightUtc;
        TimeSpan step = TimeSpan.FromMinutes(30);

        // Act
        now = now.Add(step);
        device.update(now, step, null);
        long energyAfterFirstStep = device.Header.EnergyWh;
        now = now.Add(step);
        device.update(now, step, null);

        // Assert
        Assert.Equal(5500L, energyAfterFirstStep);
        Assert.Equal(11000L, device.Header.EnergyWh);
    }

    [Fact]
    public void Update_ZeroStep_ChangesNothing()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(50);
        device.connectCar(1, car);

        // Act
        device.update(ChargerTestSupport.MidnightUtc, TimeSpan.Zero, null);

        // Assert
        Assert.Equal(50.0, car.StateOfChargePercent, 6);
        Assert.Equal(0L, device.Header.EnergyWh);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_NegativeStep_Throws()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        Action action = delegate
        {
            device.update(ChargerTestSupport.MidnightUtc, TimeSpan.FromSeconds(-1), null);
        };

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Update_PowerLimitFiftyPercent_HalvesTotalPowerAndReportsLimited()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));
        device.connectCar(2, ChargerTestSupport.createCar(50));
        device.applyCommand(new DeviceCommand(CommonCommands.SET_LIMIT, 50));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(-11000, device.Header.ActivePowerW);
        Assert.Equal(DeviceState.Limited, device.Header.State);
        Assert.Equal(50.0, device.Header.PowerLimitPercent, 6);
    }

    [Fact]
    public void Update_WithoutCar_StateIsStandby()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(DeviceState.Standby, device.Header.State);
        Assert.Equal(0, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_Charging_StateIsRunning()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.True(device.Header.IsOn);
    }

    [Fact]
    public void Update_ChargerOff_DoesNotCharge()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(60);
        device.connectCar(1, car);
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(DeviceState.Off, device.Header.State);
        Assert.False(device.Header.IsOn);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(60.0, car.StateOfChargePercent, 6);
        Assert.Equal(3.0, ChargerTestSupport.getValue(device, "Port1Status"), 6);
    }

    [Fact]
    public void Update_ChargerFault_StopsBothPorts()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(60));
        device.connectCar(2, ChargerTestSupport.createCar(60));
        device.applyCommand(new DeviceCommand(CommonCommands.TRIGGER_FAULT, 0));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(DeviceState.Fault, device.Header.State);
        Assert.Equal(ChargerDevice.FAULT_CODE_CHARGER, device.Header.FaultCode);
        Assert.Equal(0, device.Header.ActivePowerW);
        Assert.Equal(3.0, ChargerTestSupport.getValue(device, "Port1Status"), 6);
        Assert.Equal(3.0, ChargerTestSupport.getValue(device, "Port2Status"), 6);
    }

    [Theory]
    [InlineData(1, 2, "Port2PowerW", "Port1PowerW")]
    [InlineData(2, 3, "Port1PowerW", "Port2PowerW")]
    public void Update_PortFault_StopsOnlyThatPort(int faultPort, ushort expectedCode, string chargingRegister, string faultedRegister)
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(60));
        device.connectCar(2, ChargerTestSupport.createCar(60));
        device.applyCommand(new DeviceCommand("FaultPort", faultPort));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(expectedCode, device.Header.FaultCode);
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.Equal(11000.0, ChargerTestSupport.getValue(device, chargingRegister), 0);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, faultedRegister), 0);
        Assert.Equal(-11000, device.Header.ActivePowerW);
    }

    [Fact]
    public void Update_ChargerOffThenOn_ResumesCharging()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(60));
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_OFF, 0));
        device.applyCommand(new DeviceCommand(CommonCommands.TURN_ON, 0));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Assert
        Assert.Equal(DeviceState.Running, device.Header.State);
        Assert.Equal(-11000, device.Header.ActivePowerW);
    }
}
