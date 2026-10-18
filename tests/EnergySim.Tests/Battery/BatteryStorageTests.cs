using EnergySim.Battery;

namespace EnergySim.Tests.Battery;

public class BatteryStorageTests
{
    private static readonly TimeSpan ONE_HOUR = TimeSpan.FromHours(1);

    [Fact]
    public void Constructor_SetsInitialSocAndTemperature()
    {
        // Arrange + Act
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Assert
        Assert.Equal(50.0, storage.SocPercent, 6);
        Assert.Equal(25.0, storage.TemperatureC, 6);
        Assert.Equal(BatteryOperatingState.Standby, storage.OperatingState);
    }

    [Fact]
    public void Advance_Charging_StoresEnergyWithEfficiency()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, -10.0);

        // Assert
        Assert.Equal(-10.0, storage.ActualPowerKW, 6);
        Assert.Equal(10.0, storage.CumulativeChargedEnergyKWh, 6);
        Assert.Equal(59.5, storage.StoredEnergyKWh, 6); // 50 + 10 * 0.95
        Assert.Equal(BatteryOperatingState.Charging, storage.OperatingState);
    }

    [Fact]
    public void Advance_Discharging_DrawsMoreThanDelivered()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, 10.0);

        // Assert
        Assert.Equal(10.0, storage.ActualPowerKW, 6);
        Assert.Equal(10.0, storage.CumulativeDischargedEnergyKWh, 6);
        Assert.Equal(50.0 - 10.0 / 0.95, storage.StoredEnergyKWh, 6);
        Assert.Equal(BatteryOperatingState.Discharging, storage.OperatingState);
    }

    [Fact]
    public void Advance_ChargingAtMaximumSoc_StopsAtWindow()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, -50.0);
        storage.advance(ONE_HOUR, -50.0);

        // Assert
        Assert.Equal(90.0, storage.SocPercent, 6);
        Assert.Equal(0.0, storage.ActualPowerKW, 6);
        Assert.Equal(BatteryOperatingState.Standby, storage.OperatingState);
        Assert.Equal(40.0 / 0.95, storage.CumulativeChargedEnergyKWh, 6);
    }

    [Fact]
    public void Advance_DischargingAtMinimumSoc_StopsAtWindow()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, 50.0); // limited to 38 kW by available energy
        double firstPowerKw = storage.ActualPowerKW;
        storage.advance(ONE_HOUR, 50.0);

        // Assert
        Assert.Equal(38.0, firstPowerKw, 6);
        Assert.Equal(10.0, storage.SocPercent, 6);
        Assert.Equal(0.0, storage.ActualPowerKW, 6);
        Assert.Equal(38.0, storage.CumulativeDischargedEnergyKWh, 6);
    }

    [Fact]
    public void Advance_RequestAboveMaximum_IsClamped()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(TimeSpan.FromMinutes(6), 500.0);

        // Assert
        Assert.Equal(50.0, storage.ActualPowerKW, 6);
    }

    [Fact]
    public void Advance_PowerLimit_ScalesMaximumPower()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());
        storage.PowerLimitFactor = 0.5;

        // Act
        storage.advance(TimeSpan.FromMinutes(6), 40.0);

        // Assert
        Assert.Equal(25.0, storage.ActualPowerKW, 6);
        Assert.True(storage.IsPowerLimited);
    }

    [Fact]
    public void Advance_UnderLoad_RaisesTemperature()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, 10.0);

        // Assert
        Assert.Equal(25.0 + (22.0 + 0.2 * 20.0 - 25.0) * 0.5, storage.TemperatureC, 6);
    }

    [Fact]
    public void Advance_Idle_CoolsTowardAmbient()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(ONE_HOUR, 0.0);

        // Assert
        Assert.Equal(23.5, storage.TemperatureC, 6);
    }

    [Fact]
    public void Advance_ReachingFaultTemperature_RaisesFaultAndStopsPower()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(HotOptions());

        // Act
        storage.advance(ONE_HOUR, 40.0);

        // Assert
        Assert.Equal(BatteryFault.Overtemperature, storage.Fault);
        Assert.Equal(0.0, storage.ActualPowerKW, 6);
        Assert.Equal(BatteryOperatingState.Faulted, storage.OperatingState);
    }

    [Fact]
    public void Advance_WhenFaulted_IgnoresRequests()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());
        storage.triggerOvertemperatureFault();

        // Act
        storage.advance(ONE_HOUR, 20.0);

        // Assert
        Assert.Equal(0.0, storage.ActualPowerKW, 6);
        Assert.Equal(0.0, storage.CumulativeDischargedEnergyKWh, 6);
    }

    [Fact]
    public void TryResetFault_WhileHot_IsRefused()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());
        storage.triggerOvertemperatureFault();

        // Act
        bool reset = storage.tryResetFault();

        // Assert
        Assert.False(reset);
        Assert.Equal(BatteryFault.Overtemperature, storage.Fault);
    }

    [Fact]
    public void TryResetFault_AfterCooling_ClearsFault()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());
        storage.triggerOvertemperatureFault(); // 52 degC
        storage.advance(ONE_HOUR, 0.0); // 37 degC, below reset 40

        // Act
        bool reset = storage.tryResetFault();

        // Assert
        Assert.True(reset);
        Assert.Equal(BatteryFault.None, storage.Fault);
    }

    [Fact]
    public void Advance_ZeroStep_KeepsEnergy()
    {
        // Arrange
        BatteryStorage storage = new BatteryStorage(new BatteryOptions());

        // Act
        storage.advance(TimeSpan.Zero, 20.0);

        // Assert
        Assert.Equal(50.0, storage.SocPercent, 6);
        Assert.Equal(0.0, storage.CumulativeDischargedEnergyKWh, 6);
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BatteryStorage(null!));
    }

    private static BatteryOptions HotOptions()
    {
        BatteryOptions options = new BatteryOptions();
        options.WarningTemperatureC = 25;
        options.FaultTemperatureC = 30;
        options.ResetTemperatureC = 24;
        return options;
    }
}
