using EnergySim.Core.Weather;
using EnergySim.Weather;

namespace EnergySim.Tests.Weather;

public class LocationCatalogTests
{
    [Theory]
    [InlineData("salzburg", 47.80, 13.04)]
    [InlineData("wien", 48.21, 16.37)]
    [InlineData("innsbruck", 47.26, 11.39)]
    public void Get_KnownId_ReturnsCoordinates(string id, double latitude, double longitude)
    {
        // Act
        Location location = LocationCatalog.get(id);

        // Assert
        Assert.Equal(latitude, location.Latitude);
        Assert.Equal(longitude, location.Longitude);
        Assert.Equal("Europe/Vienna", location.TimeZoneDisplayName);
    }

    [Fact]
    public void Get_UnknownId_Throws()
    {
        // Act
        Action act = () => LocationCatalog.get("atlantis");

        // Assert
        Assert.Throws<UnknownLocationException>(act);
    }
}
