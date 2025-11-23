using Azure.Messaging.ServiceBus;
using MessageProcessor.Worker;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

// HttpClient for fake Greetings API – base URL from config
builder.Services.AddHttpClient<IGreetingsClient, GreetingsClient>((_, client) =>
{
    var greetingsApiBaseAddress = configuration["GreetingsApi:BaseAddress"]
                                  ?? throw new InvalidOperationException(
                                      "Missing required configuration value: 'GreetingsApi:BaseAddress'.");

    client.BaseAddress = new Uri(greetingsApiBaseAddress);
});

// ServiceBusClient – connection string from config
builder.Services.AddSingleton<ServiceBusClient>(_ =>
{
    var connectionString =
        configuration.GetConnectionString("ServiceBus") // ConnectionStrings:ServiceBus
        ?? configuration["ServiceBus:ConnectionString"] // fallback if you prefer that shape
        ?? throw new InvalidOperationException(
            "Service Bus connection string is not configured. " +
            "Set ConnectionStrings:ServiceBus in config or env.");

    return new ServiceBusClient(connectionString);
});

// ServiceBusMessageProcessor – topic/subscription from config
builder.Services.AddSingleton<ServiceBusMessageProcessor>(sp =>
{
    var sbClient = sp.GetRequiredService<ServiceBusClient>();
    var apiClient = sp.GetRequiredService<IGreetingsClient>();

    var topicName = configuration["ServiceBus:TopicName"]
                    ?? throw new InvalidOperationException("ServiceBus:TopicName is not configured.");

    var subscriptionName = configuration["ServiceBus:SubscriptionName"]
                           ?? throw new InvalidOperationException("ServiceBus:SubscriptionName is not configured.");

    return new ServiceBusMessageProcessor(sbClient, topicName, subscriptionName, apiClient);
});

// Hosted worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();