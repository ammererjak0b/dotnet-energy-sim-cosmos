using EnergySim.Battery;
using EnergySim.Charger;
using EnergySim.Core.Devices;
using EnergySim.Modbus;
using EnergySim.Pv;
using EnergySim.Wind;

namespace EnergySim.Tests.Modbus;

/// <summary>Every real device reports its SpecificValues in the order of its DeviceProfiles table.</summary>
public sealed class DeviceProfilesTests
{
    private static ISimulatedDevice createDevice(DeviceType type)
    {
        if (type == DeviceType.Pv)
        {
            return new PvDevice("pv-1", new PvOptions());
        }

        if (type == DeviceType.Battery)
        {
            return new BatteryDevice("battery-1", new BatteryOptions());
        }

        if (type == DeviceType.Charger)
        {
            return new ChargerDevice("charger-1", new ChargerOptions());
        }

        return new WindDevice("wind-1", new WindOptions());
    }

    [Theory]
    [InlineData(DeviceType.Pv)]
    [InlineData(DeviceType.Battery)]
    [InlineData(DeviceType.Charger)]
    [InlineData(DeviceType.Wind)]
    public void SpecificValues_RealDevice_MatchesProfileNamesAndOrder(DeviceType type)
    {
        // Arrange
        ISimulatedDevice device = createDevice(type);
        IReadOnlyList<DeviceProfileValue> profile = DeviceProfiles.forType(type);

        // Act
        IReadOnlyList<DeviceValue> values = device.SpecificValues;

        // Assert
        Assert.Equal(type, device.Type);
        Assert.Equal(profile.Count, values.Count);
        for (int index = 0; index < profile.Count; index++)
        {
            Assert.Equal(profile[index].Name, values[index].Name);
        }
    }

    [Theory]
    [InlineData(DeviceType.Pv, 4)]
    [InlineData(DeviceType.Battery, 4)]
    [InlineData(DeviceType.Charger, 4)]
    [InlineData(DeviceType.Wind, 3)]
    public void ForType_Type_HasContractedValueCount(DeviceType type, int expectedCount)
    {
        // Act
        IReadOnlyList<DeviceProfileValue> profile = DeviceProfiles.forType(type);

        // Assert
        Assert.Equal(expectedCount, profile.Count);
    }
}
