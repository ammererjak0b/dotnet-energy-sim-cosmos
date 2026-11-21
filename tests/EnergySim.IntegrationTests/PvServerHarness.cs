using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using EnergySim.Core.Devices;
using EnergySim.Core.Engine;
using EnergySim.Core.Weather;
using EnergySim.Modbus;
using EnergySim.Pv;

namespace EnergySim.IntegrationTests;

/// <summary>
/// A started device Modbus server (real TCP socket on a free local port) with a real PV model behind it.
/// The weather sample is fixed: 800 W/m2, 20 degC ambient gives cell 40 degC, derating 0.94, 5 kWp x 0.8 x 0.94 = 3.008 kW.
/// </summary>
public sealed class PvServerHarness : IDisposable
{
    public const string HOST = "127.0.0.1";
    public const byte UNIT_ID = 1;
    public const int POWER_W = 3008;
    private const int START_TIMEOUT_SECONDS = 10; // server must listen in time
    private static readonly DateTimeOffset NOON = new DateTimeOffset(2026, 6, 21, 12, 0, 0, TimeSpan.Zero);

    private readonly DeviceModbusServer _server;
    private readonly PvDevice _device = new PvDevice("pv-it", new PvOptions());

    private PvServerHarness(DeviceModbusOptions options)
    {
        Port = options.Port;
        Commands = Channel.CreateUnbounded<DeviceCommand>();
        _server = new DeviceModbusServer(options, Commands.Writer);
    }

    public int Port { get; }

    /// <summary>Commands the server queued for the device (what a client write became).</summary>
    public Channel<DeviceCommand> Commands { get; }

    public static DateTimeOffset VirtualTime
    {
        get
        {
            return NOON;
        }
    }

    public static async Task<PvServerHarness> startAsync()
    {
        DeviceModbusOptions options = new DeviceModbusOptions();
        options.Host = HOST;
        options.Port = findFreePort();
        options.UnitId = UNIT_ID;
        options.DeviceType = DeviceType.Pv;
        PvServerHarness harness = new PvServerHarness(options);
        using CancellationTokenSource startTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(START_TIMEOUT_SECONDS));
        try
        {
            await harness._server.startAsync(startTimeout.Token);
        }
        catch
        {
            harness.Dispose(); // no leaked server when the start fails
            throw;
        }

        return harness;
    }

    /// <summary>Real PV model at noon with the fixed weather sample, published like the simulation loop does.</summary>
    public void publishMiddayFrame(uint sequence)
    {
        WeatherDto weather = new WeatherDto();
        weather.LocationId = "salzburg";
        weather.GlobalIrradianceWm2 = 800.0;
        weather.TemperatureC = 20.0;
        weather.SunElevationDeg = 60.0;
        _device.update(NOON, TimeSpan.FromMinutes(5), weather);

        SimulationFrame frame = new SimulationFrame();
        frame.VirtualNow = NOON;
        frame.Sequence = sequence;
        frame.Header = _device.Header;
        frame.SpecificValues = _device.SpecificValues;
        frame.Snapshot = new ClockSnapshot();
        frame.Snapshot.VirtualNow = NOON;
        frame.Snapshot.Status = ClockStatus.Running;
        frame.Snapshot.IsSynced = true;
        frame.Snapshot.Generation = 1;
        _server.updateFrame(frame);
    }

    /// <summary>Closes the port. Safe to call twice.</summary>
    public void Dispose()
    {
        _server.Dispose();
    }

    /// <summary>Asks the OS for a free port. A tiny race remains between close and server start, tests would then fail loudly.</summary>
    private static int findFreePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
