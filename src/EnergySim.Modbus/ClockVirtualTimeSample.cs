namespace EnergySim.Modbus;

/// <summary>Device virtual time and the monotonic instant at which the device sampled it.</summary>
public sealed class ClockVirtualTimeSample
{
    public long VirtualUnixMilliseconds { get; set; } // 0 = device never published
    public long SampledTimestamp { get; set; } // device TimeProvider counter, same machine as master
}
