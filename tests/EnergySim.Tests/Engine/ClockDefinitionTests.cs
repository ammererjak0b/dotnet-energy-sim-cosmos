using EnergySim.Core.Engine;

namespace EnergySim.Tests.Engine;

/// <summary>Shared counter calculations without a wall clock or network.</summary>
public sealed class ClockDefinitionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(3601)]
    public void Validate_InvalidSpeed_Throws(double speed)
    {
        // Arrange
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.CounterFrequency = 1000;
        definition.Speed = speed;

        // Act
        Action validate = delegate { definition.validate(); };

        // Assert
        Assert.Throws<ArgumentException>(validate);
    }

    [Fact]
    public void GetSnapshot_Running_UsesCounterDifferenceAndSpeed()
    {
        // Arrange
        ClockDefinition definition = new ClockDefinition();
        definition.SessionId = Guid.NewGuid();
        definition.VirtualAnchor = DateTimeOffset.UnixEpoch;
        definition.CounterFrequency = 1000;
        definition.AnchorTimestamp = 5000;
        definition.Speed = 60;
        definition.Status = ClockStatus.Running;

        // Act
        ClockSnapshot snapshot = definition.getSnapshot(5500);

        // Assert
        Assert.Equal(DateTimeOffset.UnixEpoch.AddSeconds(30), snapshot.VirtualNow);
    }
}
