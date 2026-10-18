using EnergySim.Core.Weather;
using EnergySim.Weather;
using EnergySim.Pv;

namespace EnergySim.Tests.Pv;

public class PvPlantTests
{
    private static PvPlant createPlant()
    {
        return new PvPlant(new PvOptions());
    }

    [Fact]
    public void Update_AtNight_ProducesNothingAndIsStandby()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createNightWeather());

        // Assert
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(PvPlantStatus.Standby, plant.Status);
    }

    [Fact]
    public void Update_AtMidday_FollowsPowerFormula()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(3.008, plant.PowerKw, 6);
        Assert.Equal(40.0, plant.CellTemperatureC, 6);
        Assert.Equal(PvPlantStatus.Running, plant.Status);
    }

    [Fact]
    public void Update_WithVeryHotCell_NeverGoesNegative()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createWeather(1000.0, 300.0));

        // Assert
        Assert.Equal(0.0, plant.PowerKw);
    }

    [Fact]
    public void Update_WithPowerLimit_CapsPowerAndIsLimited()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.setPowerLimit(10.0);

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(0.5, plant.PowerKw, 6);
        Assert.Equal(PvPlantStatus.Limited, plant.Status);
    }

    [Fact]
    public void Update_WithLimitAboveFreePower_StaysRunning()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.setPowerLimit(90.0);

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(3.008, plant.PowerKw, 6);
        Assert.Equal(PvPlantStatus.Running, plant.Status);
    }

    [Fact]
    public void Fault_StopsPowerUntilReset()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Act
        plant.triggerFault();
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());
        double faultPowerKw = plant.PowerKw;
        PvPlantStatus faultStatus = plant.Status;
        plant.resetFault();
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(0.0, faultPowerKw);
        Assert.Equal(PvPlantStatus.Fault, faultStatus);
        Assert.True(plant.PowerKw > 0.0);
        Assert.Equal(PvPlantStatus.Running, plant.Status);
    }

    [Fact]
    public void TurnOffAndOn_SwitchesPower()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.turnOff();

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());
        double offPowerKw = plant.PowerKw;
        PvPlantStatus offStatus = plant.Status;
        plant.turnOn();
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(0.0, offPowerKw);
        Assert.Equal(PvPlantStatus.Off, offStatus);
        Assert.True(plant.PowerKw > 0.0);
    }

    [Fact]
    public void Update_AddsEnergyToDailyAndTotal()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act
        plant.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(3008.0, plant.TotalEnergyWh, 6);
        Assert.Equal(3008.0, plant.EnergyTodayWh, 6);
    }

    [Fact]
    public void Update_AtUtcMidnight_ResetsDailyEnergyAndKeepsTotal()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());
        DateTimeOffset nextMidnight = new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero);

        // Act
        plant.update(nextMidnight, TimeSpan.Zero, PvTestData.createNightWeather());

        // Assert
        Assert.Equal(0.0, plant.EnergyTodayWh);
        Assert.Equal(3008.0, plant.TotalEnergyWh, 6);
    }

    [Fact]
    public void Update_WithOffsetTime_UsesUtcDay()
    {
        // Arrange
        PvPlant plant = createPlant();
        DateTimeOffset localLate = new DateTimeOffset(2026, 6, 21, 23, 30, 0, TimeSpan.FromHours(2)); // 21:30 UTC
        plant.update(localLate, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());
        DateTimeOffset localNextDay = new DateTimeOffset(2026, 6, 22, 0, 30, 0, TimeSpan.FromHours(2)); // 22:30 UTC, same UTC day

        // Act
        plant.update(localNextDay, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(6016.0, plant.EnergyTodayWh, 6);
    }

    [Fact]
    public void Update_WithZeroStep_KeepsEnergy()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());

        // Act
        plant.update(PvTestData.NOON, TimeSpan.Zero, PvTestData.createMiddayWeather());

        // Assert
        Assert.Equal(3008.0, plant.TotalEnergyWh, 6);
    }

    [Fact]
    public void Update_WithNegativeStep_Throws()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act, Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => plant.update(PvTestData.NOON, TimeSpan.FromSeconds(-1), PvTestData.createMiddayWeather()));
    }

    [Fact]
    public void Update_WithoutWeather_ProducesNothingAndIsStandby()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.update(PvTestData.NOON, PvTestData.STEP, PvTestData.createMiddayWeather());

        // Act
        plant.update(PvTestData.NOON, PvTestData.STEP, null);

        // Assert
        Assert.Equal(0.0, plant.PowerKw);
        Assert.Equal(PvPlantStatus.Standby, plant.Status);
        Assert.False(plant.HasWeather);
        Assert.False(plant.HasFault);
    }

    [Fact]
    public void SetPowerLimit_OutOfRange_Throws()
    {
        // Arrange
        PvPlant plant = createPlant();

        // Act, Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => plant.setPowerLimit(150.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => plant.setPowerLimit(-1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => plant.setPowerLimit(double.NaN));
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        PvPlant plant = createPlant();
        plant.update(PvTestData.NOON, TimeSpan.FromHours(1), PvTestData.createMiddayWeather());
        plant.triggerFault();
        plant.setPowerLimit(20.0);
        plant.turnOff();

        // Act
        plant.reset(PvTestData.NOON);

        // Assert
        Assert.Equal(0.0, plant.TotalEnergyWh);
        Assert.Equal(0.0, plant.EnergyTodayWh);
        Assert.Equal(0.0, plant.PowerKw);
        Assert.False(plant.HasFault);
        Assert.True(plant.IsOn);
        Assert.Equal(100.0, plant.PowerLimitPercent);
    }
}
