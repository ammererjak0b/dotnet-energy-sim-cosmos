using EnergySim.Battery;

namespace EnergySim.Tests.Battery;

public class BatteryOptionsTests
{
    [Fact]
    public void Defaults_MatchLb01Settings()
    {
        // Arrange
        BatteryOptions options = new BatteryOptions();

        // Act
        options.validate();

        // Assert
        Assert.Equal("Battery", BatteryOptions.SECTION_NAME);
        Assert.Equal("LFP", options.Technology);
        Assert.Equal(100.0, options.CapacityKWh);
        Assert.Equal(50.0, options.MaxChargePowerKW);
        Assert.Equal(50.0, options.MaxDischargePowerKW);
        Assert.Equal(50.0, options.InitialSocPercent);
        Assert.Equal(10.0, options.MinimumSocPercent);
        Assert.Equal(90.0, options.MaximumSocPercent);
        Assert.Equal(95.0, options.ChargeEfficiencyPercent);
        Assert.Equal(95.0, options.DischargeEfficiencyPercent);
        Assert.Equal(25.0, options.InitialTemperatureC);
        Assert.Equal(22.0, options.AmbientTemperatureC);
        Assert.Equal(40.0, options.WarningTemperatureC);
        Assert.Equal(50.0, options.FaultTemperatureC);
        Assert.Equal(40.0, options.ResetTemperatureC);
        Assert.Equal(42, options.RandomSeed);
    }

    [Fact]
    public void Validate_NonPositiveCapacity_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.CapacityKWh = 0;

        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Fact]
    public void Validate_NonPositivePower_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.MaxChargePowerKW = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());

        options = new BatteryOptions();
        options.MaxDischargePowerKW = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Fact]
    public void Validate_SocWindowInverted_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.MinimumSocPercent = 90;
        options.MaximumSocPercent = 10;
        options.InitialSocPercent = 50;

        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Fact]
    public void Validate_InitialSocOutsideWindow_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.InitialSocPercent = 95;

        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(101.0)]
    public void Validate_EfficiencyOutOfRange_Throws(double efficiencyPercent)
    {
        BatteryOptions options = new BatteryOptions();
        options.ChargeEfficiencyPercent = efficiencyPercent;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());

        options = new BatteryOptions();
        options.DischargeEfficiencyPercent = efficiencyPercent;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Fact]
    public void Validate_TemperaturesMisordered_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.ResetTemperatureC = 50;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());

        options = new BatteryOptions();
        options.WarningTemperatureC = 60;
        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }

    [Fact]
    public void Validate_NaN_Throws()
    {
        BatteryOptions options = new BatteryOptions();
        options.AmbientTemperatureC = double.NaN;

        Assert.Throws<ArgumentOutOfRangeException>(() => options.validate());
    }
}
