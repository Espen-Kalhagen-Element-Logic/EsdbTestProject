using EsdbEvents;
using EventStore.Client;
using Microsoft.Extensions.Logging;

namespace EsdbTestProject1;
public class SubscriptionHandler
{
    private readonly EventStoreSubscriber _eventStoreSubscriber;
    private readonly ILogger<SubscriptionHandler> _logger;

    public SubscriptionHandler(EventStoreSubscriber eventStoreSubscriber, ILogger<SubscriptionHandler> logger)
    {
        _eventStoreSubscriber = eventStoreSubscriber;
        _logger = logger;
    }

    public async Task SubscribeToStream(string streamName, Func<ResolvedEvent, StreamEvent, Task> action, CancellationToken cancellationToken)
    {
        var stream = await _eventStoreSubscriber.SubscribeStreaEventAsync(streamName, async (evnt, data) =>
        {
            _logger.LogDebug("{eventType} event received", evnt.Event.EventType);
            await action(evnt, data);
        },
        cancellationToken
        );
    }
}
