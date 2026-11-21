using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnergySim.Messaging;

/// <summary>One JSON setup for publisher and viewer: camelCase, enums as text.</summary>
public static class TelemetryJson
{
    private static readonly JsonSerializerOptions OPTIONS = createOptions();

    public static byte[] serialize(TelemetryMessage message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, OPTIONS);
    }

    /// <summary>Throws JsonException for invalid content.</summary>
    public static TelemetryMessage deserialize(ReadOnlyMemory<byte> body)
    {
        TelemetryMessage? message = JsonSerializer.Deserialize<TelemetryMessage>(body.Span, OPTIONS);
        if (message == null)
        {
            throw new JsonException("message body is empty");
        }

        return message;
    }

    private static JsonSerializerOptions createOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
