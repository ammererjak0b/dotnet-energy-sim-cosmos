using EnergySim.Core.Devices;
using EnergySim.Charger;

namespace EnergySim.Tests.Charger;

public class ChargerDisplayTests
{
    [Fact]
    public void CsvHistoryFields_UseLegacyColumnsAndCurrentChargerValues()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(50);
        car.Name = "Zoe";
        device.connectCar(1, car);
        device.update(ChargerTestSupport.MidnightUtc.AddHours(1), TimeSpan.FromHours(1), null);

        // Act
        IReadOnlyList<EnergySim.Core.History.CsvHistoryField> fields = device.CsvHistoryFields;

        // Assert
        Assert.Equal("simulationTime", fields[0].Name);
        Assert.Equal("01:00:00", fields[0].Value);
        Assert.Equal("On", fields[1].Value);
        Assert.Equal("port1CarId", fields[5].Name);
        Assert.Equal("Zoe", fields[6].Value);
        Assert.Equal("11.00", fields[7].Value);
        Assert.Equal("11.00", fields[8].Value);
        Assert.Equal(19, fields.Count);
    }

    [Fact]
    public void Device_Identity_IsChargerWithoutWeather()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Assert
        Assert.Equal(ChargerTestSupport.DEVICE_ID, device.DeviceId);
        Assert.Equal(DeviceType.Charger, device.Type);
        Assert.False(device.NeedsWeather);
    }

    [Fact]
    public void PortValues_WithCars_ShowPowerAndSoc()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(11000.0, ChargerTestSupport.getValue(device, "Port1PowerW"), 0);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2PowerW"), 0);
        Assert.Equal(68.3, ChargerTestSupport.getValue(device, "Port1SocPercent"), 1);
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port2SocPercent"), 6);
    }

    [Fact]
    public void PortValues_PortStatus_FollowsMapping()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.connectCar(1, ChargerTestSupport.createCar(50));
        device.applyCommand(new DeviceCommand("FaultPort", 2));

        // Act
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(1));

        // Assert
        Assert.Equal(1.0, ChargerTestSupport.getValue(device, "Port1Status"), 6); // charging
        Assert.Equal(2.0, ChargerTestSupport.getValue(device, "Port2Status"), 6); // fault
    }

    [Fact]
    public void PortValues_FreePort_IsAvailable()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();

        // Assert
        Assert.Equal(0.0, ChargerTestSupport.getValue(device, "Port1Status"), 6);
    }

    [Fact]
    public void DisplayRows_ShowPortsAndTotals()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        ECar car = ChargerTestSupport.createCar(50);
        car.Name = "Zoe";
        device.connectCar(1, car);
        ChargerTestSupport.advance(device, TimeSpan.FromMinutes(15));

        // Act
        IReadOnlyList<DisplayRow> rows = device.DisplayRows;

        // Assert
        Assert.Equal(4, rows.Count);
        Assert.Contains("Zoe", rows[0].Label);
        Assert.True(rows[0].HasBar);
        Assert.InRange(rows[0].BarFraction, 0.5, 0.6);
        Assert.Contains("11.0 kW", rows[0].Value);
        Assert.False(rows[1].HasBar);
        Assert.Contains("free", rows[1].Value);
        Assert.Contains("11.0 kW", rows[2].Value);
    }

    [Fact]
    public void DisplayRows_PortFault_HasFaultSeverity()
    {
        // Arrange
        ChargerDevice device = ChargerTestSupport.createDevice();
        device.applyCommand(new DeviceCommand("FaultPort", 2));

        // Act
        IReadOnlyList<DisplayRow> rows = device.DisplayRows;

        // Assert
        Assert.Equal(DisplaySeverity.Normal, rows[0].Severity);
        Assert.Equal(DisplaySeverity.Fault, rows[1].Severity);
    }

    [Fact]
    public void Options_Defaults_MatchModel()
    {
        // Arrange
        ChargerOptions options = new ChargerOptions();

        // Assert
        Assert.Equal("Charger", ChargerOptions.SECTION_NAME);
        Assert.Equal(11.0, options.Port1MaxPowerKw);
        Assert.Equal(11.0, options.Port2MaxPowerKw);
        Assert.Equal(22.0, options.TotalMaxPowerKw);
        Assert.Equal(80.0, options.TaperStartSocPercent);
        Assert.Equal(1.0, options.MinimumTaperPowerKw);
        Assert.True(options.Schedule.Count >= 2);
    }
}
