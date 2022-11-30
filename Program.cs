using eOperator.CommunicationHub.EventStore;
using EsdbTestProject1;
using EventStore.Client;
using Serilog;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .AddEnvironmentVariables()
    .Build();

const string moduleName = "testproject1";

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("module", moduleName)
    .CreateLogger();

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton(Log.Logger);

        services.AddTransient<EventStoreSubscriber>();
        services.AddTransient<SubscriptionHandler>();
        services.AddTransient<EventStorePublisher>();
        services.AddLogging(l => l.AddSerilog());

        var esdbConnectionString = configuration.GetValue("ESDB_CONNECTION_STRING", "esdb://admin:changeit@localhost:2113?tls=false");
        var eventStoreClientSettings = EventStoreClientSettings.Create(esdbConnectionString);
        var eventStoreClient = new EventStoreClient(eventStoreClientSettings);
        services.AddSingleton(eventStoreClient);

        var provider = services.BuildServiceProvider();

        Console.WriteLine("Enter run id, this needs to be a uniqe string among all runs and is used to prevent streamname craches");
        var runId = Console.ReadLine();
        if(runId == null)
        {
            throw new ArgumentException("you need to enter runid");
        }
        services.AddHostedService(x => new Orchestrator(provider.GetRequiredService<EventStorePublisher>(), provider, provider.GetRequiredService<ILogger<Orchestrator>>(), runId, false));
    })
    .Build();

await host.RunAsync();
