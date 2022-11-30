using EsdbEvents;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EsdbTestProject1;

internal static class StreamStoreHelpers
{
    private static readonly Assembly _assembly = Assembly.GetAssembly(typeof(StreamEvent))!;

    public static async Task<StreamEvent> ReadFromStartOfStreamToEnd(EventStoreClient client, string streamName, ILogger? logger = null, ResolvedEvent? evnt = null)
    {
        var aggregate = new StreamEvent();
        try
        {
            logger?.LogTrace("Starting to read from {streamName}", streamName);
            var readStreamResult = client.ReadStreamAsync(
                        Direction.Forwards,
                        streamName,
                        StreamPosition.Start);

            await foreach (var singularEvent in readStreamResult)
            {
                logger?.LogTrace("Reading event of type {eventType}", singularEvent.Event.EventType);
                var eventType = GetEventTypeNameSpace(singularEvent.Event.EventType);
                var foundType = _assembly.GetType(eventType);
                if (foundType == null)
                {
                    logger?.LogWarning("We couldn't find the type for {eventType}", eventType);
                    continue;
                }

                logger?.LogTrace("Attempting to deserialize data of type {dataType}", foundType.Name);

                var eventData = singularEvent.Deserialize(foundType);

                if (eventData != null)
                    aggregate.When(eventData);
                else
                {
                    logger?.LogWarning("Event data is null!? This might be an issue, happened while trying to read a {eventType}", eventType);
                    throw new NullReferenceException(eventType);
                }

                if (evnt != null && evnt.Value.Event.EventId == singularEvent.Event.EventId)
                {
                    logger?.LogTrace("Reached sent in event number, will stop reading");
                    break;
                }
            }
        }
        catch (Exception e)
        {
            logger?.LogWarning("Exception happened during reading of stream: {streamName} {@exception}", streamName, e);
        }
        return aggregate;
    }

    public static string GetEventTypeNameSpace(string eventType)
    {
        var ourNameSpace = typeof(StreamEvent).Namespace;
        return $"{ourNameSpace}.{eventType}";
    }
}
