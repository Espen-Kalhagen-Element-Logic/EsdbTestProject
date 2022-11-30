using EsdbTestProject1;
using EventStore.Client;

namespace eOperator.CommunicationHub.EventStore;

public class EventStorePublisher
{
    private readonly EventStoreClient _client;

    public EventStorePublisher(EventStoreClient client)
    {
        _client = client;
    }

    public Task PublishAsync<T>(string streamName, T payload, int retries = 9, double retryBaseTime = 2, int maxAgeDays = 30)
    {
        var metadata = new StreamMetadata(maxAge: new TimeSpan(maxAgeDays, 0, 0, 0));
        var eventData = new EventData(
            Uuid.NewUuid(),
            typeof(T).Name,
            EventSerialization.SerializeEventData(payload),
            EventSerialization.SerializeEventData(metadata)
        );

        return AppendToStream(streamName, retries, retryBaseTime, eventData);
    }

    private Task AppendToStream(string streamName, int retries, double retryBaseTime, EventData eventData)
    {
        return _client.AppendToStreamAsync(streamName,
           StreamState.Any,
           new[] { eventData });
    }
}
