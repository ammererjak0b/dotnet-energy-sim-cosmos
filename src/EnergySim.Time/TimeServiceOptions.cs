namespace EnergySim.Time;

/// <summary>Local Modbus and clock scheduling configuration.</summary>
public sealed class TimeServiceOptions
{
    public int Port { get; set; } = 1502;
    public int LeadTimeMilliseconds { get; set; } = 100;
    public int HeartbeatMilliseconds { get; set; } = 1000;
    public int HeartbeatExpirySeconds { get; set; } = 3;
    public string InitialTimeUtc { get; set; } = string.Empty;
    public double InitialSpeed { get; set; } = 60;

    public void validate()
    {
        if (Port < 1024 || Port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port));
        }

        if (LeadTimeMilliseconds < 25 || HeartbeatMilliseconds < 250 || HeartbeatExpirySeconds < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(LeadTimeMilliseconds));
        }

        if (!double.IsFinite(InitialSpeed) || InitialSpeed < EnergySim.Core.Engine.LocalClock.MIN_SPEED
            || InitialSpeed > EnergySim.Core.Engine.LocalClock.MAX_SPEED)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialSpeed));
        }
    }
}
