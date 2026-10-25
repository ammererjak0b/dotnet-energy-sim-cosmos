namespace EnergySim.Modbus;

/// <summary>Service endpoint used by one device clock client.</summary>
public sealed class SharedClockConnectionOptions
{
    public string ServiceHost { get; set; } = "127.0.0.1";
    public int ServicePort { get; set; } = 1502;
    public byte UnitIdentifier { get; set; } = 1;
    public int RegistrationRetryMilliseconds { get; set; } = 1000;
    public int HeartbeatExpirySeconds { get; set; } = 3;

    public void validate()
    {
        if (ServiceHost != "127.0.0.1" && ServiceHost != "localhost")
        {
            throw new ArgumentException("Time service must be on localhost.", nameof(ServiceHost));
        }

        if (ServicePort < 1024 || ServicePort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(ServicePort));
        }

        if (RegistrationRetryMilliseconds < 100 || HeartbeatExpirySeconds < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(RegistrationRetryMilliseconds));
        }
    }
}
