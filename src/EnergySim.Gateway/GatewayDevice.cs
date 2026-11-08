using EnergySim.Messaging;
using EnergySim.Modbus;

namespace EnergySim.Gateway;

/// <summary>
/// One polled device: its register client and what was last published. Checks the cancellation token around the
/// (synchronous) Modbus calls. Returns the message to publish, or null when nothing new has to go out.
/// </summary>
public sealed class GatewayDevice : IDisposable
{
    private readonly ModbusRegisterClient _client;
    private string _lastKey = string.Empty; // sequence, generation, clock state, state, fault, warnings of the last published state
    private bool _hasPublished; // false = next good state is published
    private bool _outageReported; // Bad message already sent for this outage

    public GatewayDevice(GatewayDeviceConfig config, byte unitId)
    {
        Config = config;
        _client = new ModbusRegisterClient(config.Host, config.Port, unitId);
    }

    public GatewayDeviceConfig Config { get; }

    /// <summary>Publish the current state again at the next poll (after a broker outage).</summary>
    public void forgetPublished()
    {
        _hasPublished = false;
        _outageReported = false; // a Bad that was dropped must be sent again
    }

    public TelemetryMessage? poll(DateTime polledAtUtc, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            ushort[] headerRegisters = _client.readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT); // one request
            HeaderSnapshot header = HeaderSnapshot.decode(headerRegisters);
            if (header.Type != Config.Type)
            {
                throw new HeaderDecodeException("device reports type " + header.Type + ", configured " + Config.Type, false);
            }

            string key = header.Sequence + "/" + header.Generation + "/" + header.ClockState + "/" + header.State + "/" + header.FaultCode + "/" + header.WarnFlags;
            if (_hasPublished && key == _lastKey)
            {
                _outageReported = false;
                return null; // nothing new, e.g. paused
            }

            int valueCount = DeviceProfiles.forType(header.Type).Count;
            ushort[] deviceBlock = _client.readInputRegisters(RegisterMap.SPECIFIC_START, valueCount * RegisterMap.SPECIFIC_REGISTERS_PER_VALUE);
            cancellationToken.ThrowIfCancellationRequested();
            ushort[] headerAfter = _client.readInputRegisters(0, RegisterMap.HEADER_REGISTER_COUNT); // torn read check: sequence before and after
            if (RegisterCodec.readUInt32(headerAfter, RegisterMap.IN_SEQUENCE) != header.Sequence)
            {
                return null; // state moved while reading, next poll reads a consistent one
            }

            _lastKey = key;
            _hasPublished = true;
            _outageReported = false;
            return TelemetryMapper.map(Config.Id, header, deviceBlock, polledAtUtc);
        }
        catch (ModbusRegisterException exception)
        {
            return failed(describe(exception), polledAtUtc);
        }
        catch (HeaderDecodeException exception)
        {
            return failed(exception.Message, polledAtUtc);
        }
        catch (Exception exception) when (exception is IndexOutOfRangeException || exception is ArgumentException)
        {
            return failed("unexpected register data: " + exception.Message, polledAtUtc); // short block, one device must not stop the others
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private TelemetryMessage? failed(string reason, DateTime polledAtUtc)
    {
        _hasPublished = false; // first good state after the outage is published even with the same sequence
        if (_outageReported)
        {
            return null; // Bad is sent once per outage
        }

        _outageReported = true;
        return TelemetryMapper.mapBad(Config, polledAtUtc, reason);
    }

    private string describe(ModbusRegisterException exception)
    {
        string endpoint = Config.Host + ":" + Config.Port;
        if (exception.IsProtocolError)
        {
            return "protocol error on " + endpoint + ": " + exception.Message;
        }

        if (exception.WasConnected)
        {
            return "lost connection to " + endpoint + ": " + exception.Message;
        }

        return "cannot reach " + endpoint + ": " + exception.Message;
    }
}
