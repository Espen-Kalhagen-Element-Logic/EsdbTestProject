using EventStore.Client;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EsdbTestProject1;

public static class EventSerialization
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static object? Deserialize(this ResolvedEvent resolvedEvent, Type returnType)
    {
        var eventData = JsonSerializer.Deserialize(resolvedEvent.Event.Data.ToArray(), returnType, _jsonSerializerOptions);
        return eventData;
    }

    public static T? Deserialize<T>(this ResolvedEvent resolvedEvent)
    {
        var eventData = JsonSerializer.Deserialize<T>(resolvedEvent.Event.Data.ToArray(), _jsonSerializerOptions);
        return eventData;
    }

    public static byte[] SerializeEventData<T>(T payload)
    {
        return JsonSerializer.SerializeToUtf8Bytes(payload, _jsonSerializerOptions);
    }
}
