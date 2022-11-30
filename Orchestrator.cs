using eOperator.CommunicationHub.EventStore;
using EsdbEvents;
using EventStore.Client;
using Timeout = System.Threading.Timeout;

namespace EsdbTestProject1;

public class Orchestrator : IHostedService
{
    private readonly bool _isSlave;
    private readonly ILogger<Orchestrator> _logger;
    private readonly string _runId;
    private readonly EventStorePublisher _eventStorePublisher;
    private readonly IServiceProvider _provider;
    private const int _numberOfStreams = 100000;

    private int _currentStreamNumber = 0;
    private bool _previousStreamIsDone = true;
    private Timer _timer;

    public Orchestrator(EventStorePublisher eventStorePublisher, IServiceProvider provider, ILogger<Orchestrator> logger, string runId, bool isSlave = true)
    {
        _isSlave = isSlave;
        _logger = logger;
        _runId = runId;
        _eventStorePublisher = eventStorePublisher;
        _provider = provider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stream Orchestartor is running.");

        var timerCallback = RunNextStreamMaster;

        if (_isSlave)
        {
            _timer = new Timer(RunNextStreamSlave, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
        }
        else
        {
            _timer = new Timer(RunNextStreamMaster, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stream Orchestartor is stopping.");

        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    // Async void is okey for event handlers
    public async void RunNextStreamMaster(object? _)
    {
        if(_numberOfStreams <= _currentStreamNumber)
        {
            await StopAsync(CancellationToken.None);
        }

        if (_previousStreamIsDone)
        {
            _previousStreamIsDone = false;
            var streamName = CreateStreamName();

            _logger.LogInformation("Master is creating a new stream {streamNr}", streamName);
            await _eventStorePublisher.PublishAsync(streamName, new PingEvent { StreamNr = _currentStreamNumber });

            var streamCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(30));

            // We use provider to ger a new SubscriptionHandler every time to simulate how it would work with masstransit consumers
            SubscriptionHandler subscriptonHandler = _provider.GetRequiredService<SubscriptionHandler>();
            await subscriptonHandler.SubscribeToStream(streamName, (ResolvedEvent evnt, StreamEvent aggregate) =>
            {
                if(evnt.Event.EventType == "PongEvent")
                {
                    _currentStreamNumber = aggregate.StreamNr + 1; ;
                    _previousStreamIsDone = true;
                    streamCancellationTokenSource.Cancel();
                }
                return Task.CompletedTask;
            },
            streamCancellationTokenSource.Token);
        }
    }

    // Async void is okey for event handlers
    public async void RunNextStreamSlave(object? _)
    {
        if (_numberOfStreams <= _currentStreamNumber)
        {
            await StopAsync(CancellationToken.None);
        }

        if (_previousStreamIsDone)
        {
            _previousStreamIsDone = false;
            var streamName = CreateStreamName();

            _logger.LogInformation("Slave is subscribing a new stream {streamNr}", streamName);
            var streamCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(30));

            // We use provider to ger a new SubscriptionHandler every time to simulate how it would work with masstransit consumers
            SubscriptionHandler subscriptonHandler = _provider.GetRequiredService<SubscriptionHandler>();
            await subscriptonHandler.SubscribeToStream(streamName, async (ResolvedEvent evnt, StreamEvent aggregate) =>
            {
                if (evnt.Event.EventType == "PingEvent")
                {
                    await _eventStorePublisher.PublishAsync(streamName, new PongEvent { StreamNr = _currentStreamNumber });

                    _currentStreamNumber = aggregate.StreamNr + 1; ;
                    _previousStreamIsDone = true;
                    streamCancellationTokenSource.Cancel();
                }
            },
            streamCancellationTokenSource.Token);
        }
    }

    private string CreateStreamName()
    {
        return $"{_runId}-{_currentStreamNumber}";
    }
}
