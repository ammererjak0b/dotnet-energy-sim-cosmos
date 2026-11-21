using EnergySim.Time;

namespace EnergySim.UnitTests.Engine;

/// <summary>Participants are told apart by host and port, so containers can share a default clock port.</summary>
public sealed class ClockParticipantKeyTests
{
    [Fact]
    public void CreateKey_SamePortOtherHost_IsDifferent()
    {
        // Act
        string windOne = ClockParticipant.createKey("wind-1", 1604);
        string windTwo = ClockParticipant.createKey("wind-2", 1604);

        // Assert
        Assert.NotEqual(windOne, windTwo);
    }

    [Fact]
    public void CreateKey_SameHostAndPort_IsEqual()
    {
        // Act
        string first = ClockParticipant.createKey("wind-1", 1604);
        string second = ClockParticipant.createKey("wind-1", 1604);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Key_OfParticipant_MatchesCreateKey()
    {
        // Arrange
        ClockParticipant participant = new ClockParticipant("wind-2", 1604, 1, "wind-2", DateTimeOffset.UnixEpoch);

        // Act
        string key = participant.Key;
        participant.Dispose();

        // Assert
        Assert.Equal("wind-2:1604", key);
    }
}
