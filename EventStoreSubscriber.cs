using EsdbEvents;
using EventStore.Client;

namespace EsdbTestProject1;

public class EventStoreSubscriber
{
    private readonly ILogger<EventStoreSubscriber> _logger;
    private readonly EventStoreClient _client;

    public EventStoreSubscriber(ILogger<EventStoreSubscriber> logger, EventStoreClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<StreamSubscription?> SubscribeStreaEventAsync(string streamName, Func<ResolvedEvent, StreamEvent, Task> action, CancellationToken cancellationToken)
    {

        _logger.LogTrace("Subscribing to event stream {streamName}", streamName);
        return await SubscribeToStreamAsync(
            streamName,
            FromStream.Start,
            async (sub, evnt, token) =>
            {
                await HandleEvent(streamName, evnt, sub, action);
            },
            cancellationToken
            , (subscription, dropReason, exception) =>
            {
                HandleDroppedSubscription(streamName, dropReason, exception);
            });
    }

    private Task<StreamSubscription> SubscribeToStreamAsync(string streamName, FromStream end,
       Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> action,
       CancellationToken cancellationToken,
       Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? droppedAction = null)
    {
        _logger.LogInformation("Subscribing to new stream in {method} in EventStoreWrapper {ConnectionName}", nameof(SubscribeToStreamAsync), _client.ConnectionName);

        return _client.SubscribeToStreamAsync(streamName, end, action, subscriptionDropped: droppedAction, cancellationToken: cancellationToken);
    }

    private async Task HandleEvent(string streamName, ResolvedEvent evnt, StreamSubscription sub, Func<ResolvedEvent, StreamEvent, Task> action)
    {
        var aggregate = await StreamStoreHelpers.ReadFromStartOfStreamToEnd(_client, streamName, _logger, evnt);

        await CallActionAndHandleErrors(action, evnt, aggregate);
    }

    private async Task CallActionAndHandleErrors(Func<ResolvedEvent, StreamEvent, Task> action, ResolvedEvent evnt, StreamEvent aggregate)
    {
        try
        {
            await action(evnt, aggregate);
        }
        catch (Exception e)
        {
            _logger.LogError("Major exception in action event subscription logic: {@error}", e);
        }
    }

    private void HandleDroppedSubscription(string streamName, SubscriptionDroppedReason dropReason, Exception? exception)
    {
        if (dropReason == SubscriptionDroppedReason.Disposed && exception == null)
        {
            _logger.LogDebug("Subscription has been disposed {streamName}", streamName);
        }
        else
        {
            _logger.LogError("Subscription on stream {streamName} was dropped due to {@dropReason}. {@exception}", streamName, dropReason, exception);
        }
    }
}
