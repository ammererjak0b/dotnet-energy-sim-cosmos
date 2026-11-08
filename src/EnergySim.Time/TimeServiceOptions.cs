using EnergySim.Core.Engine;
using EnergySim.Modbus;

namespace EnergySim.Time;

/// <summary>Local Modbus and clock scheduling configuration.</summary>
public sealed class TimeServiceOptions
{
    public const int MIN_LEAD_TIME_MILLISECONDS = 25;
    public const int MIN_HEARTBEAT_MILLISECONDS = 250;
    public const int MIN_HEARTBEAT_EXPIRY_SECONDS = 2;

    public int Port { get; set; } = 1502;
    public int LeadTimeMilliseconds { get; set; } = 100;
    public int HeartbeatMilliseconds { get; set; } = 1000;
    public int HeartbeatExpirySeconds { get; set; } = 3;
    public string InitialTimeUtc { get; set; } = string.Empty;
    public double InitialSpeed { get; set; } = 60;
    public bool Headless { get; set; } // no terminal, plain log lines and stdin commands

    public void validate()
    {
        if (Port < PortRules.MIN_PORT || Port > PortRules.MAX_PORT)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), Port, "Port must be " + PortRules.MIN_PORT + " to " + PortRules.MAX_PORT + ".");
        }

        if (LeadTimeMilliseconds < MIN_LEAD_TIME_MILLISECONDS)
        {
            throw new ArgumentOutOfRangeException(nameof(LeadTimeMilliseconds), LeadTimeMilliseconds, "LeadTimeMilliseconds must be at least " + MIN_LEAD_TIME_MILLISECONDS + ".");
        }

        if (HeartbeatMilliseconds < MIN_HEARTBEAT_MILLISECONDS)
        {
            throw new ArgumentOutOfRangeException(nameof(HeartbeatMilliseconds), HeartbeatMilliseconds, "HeartbeatMilliseconds must be at least " + MIN_HEARTBEAT_MILLISECONDS + ".");
        }

        if (HeartbeatExpirySeconds < MIN_HEARTBEAT_EXPIRY_SECONDS)
        {
            throw new ArgumentOutOfRangeException(nameof(HeartbeatExpirySeconds), HeartbeatExpirySeconds, "HeartbeatExpirySeconds must be at least " + MIN_HEARTBEAT_EXPIRY_SECONDS + ".");
        }

        if (!double.IsFinite(InitialSpeed) || InitialSpeed < LocalClock.MIN_SPEED || InitialSpeed > LocalClock.MAX_SPEED)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialSpeed), InitialSpeed, "InitialSpeed must be " + LocalClock.MIN_SPEED + " to " + LocalClock.MAX_SPEED + ".");
        }
    }
}
