using Azure.Messaging.ServiceBus;
using MessageProcessor.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace MessageProcessor.Tests;

[Collection("Worker Collection")]
public class ServiceBusMessageProcessorTests : IAsyncLifetime
{
    // Default Topic
    private const string TopicName = "topic.1";
    // Default Subscription
    private const string SubscriptionName = "subscription.3";
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusMessageProcessor _serviceBusMessageProcessor;
    private readonly WireMockServer _wireMockServer;

    public ServiceBusMessageProcessorTests(AzureServiceBusEmulatorFixture fixture)
    {
        _wireMockServer = fixture.WireMockServer;
        _wireMockServer.Reset();
        var wireMockUrl = _wireMockServer.Url;
        var httpClient = new HttpClient { BaseAddress = new Uri(wireMockUrl!) };
        IGreetingsClient greetingsClient = new GreetingsClient(httpClient);

        _serviceBusClient = new ServiceBusClient(fixture.ConnectionString);
        _serviceBusMessageProcessor =
            new ServiceBusMessageProcessor(_serviceBusClient, TopicName, SubscriptionName, greetingsClient,
                NullLogger<ServiceBusMessageProcessor>.Instance);
    }

    public async ValueTask InitializeAsync()
    {
        await DrainSubscriptionAsync(
            new ServiceBusReceiverOptions(),
            TestContext.Current.CancellationToken);

        await DrainSubscriptionAsync(
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter },
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Processor_RemovesMessageFromSubscription_WhenProcessingSucceeds()
    {
        // Arrange
        var sender = _serviceBusClient.CreateSender(TopicName);
        await sender.SendMessageAsync(
            new ServiceBusMessage("message"),
            TestContext.Current.CancellationToken);

        _wireMockServer.Given(Request.Create().WithPath("/hello").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("Hello!"));

        var receiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName);

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);

        // Wait until the processor has handled the message.
        await WaitUntilAsync(
            () => Task.FromResult(_wireMockServer.LogEntries.Any(entry =>
                entry.RequestMessage is { Path: "/hello", Method: "GET" })),
            timeout: TimeSpan.FromSeconds(10),
            pollInterval: TimeSpan.FromMilliseconds(50),
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        var remainingMessage = await receiver.ReceiveMessageAsync(
            TimeSpan.FromSeconds(1),
            TestContext.Current.CancellationToken);
        remainingMessage.ShouldBeNull();
    }
    
    [Fact]
    public async Task Processor_MovesMessageToDeadLetterQueue_WhenProcessingFails()
    {
        // Arrange
        _wireMockServer.Given(Request.Create().WithPath("/hello").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var sender = _serviceBusClient.CreateSender(TopicName);
        await sender.SendMessageAsync(
            new ServiceBusMessage("will fail"),
            TestContext.Current.CancellationToken);

        var dlqReceiver = _serviceBusClient.CreateReceiver(
            TopicName,
            SubscriptionName,
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);
        
        var deadLetteredMessage = await dlqReceiver.ReceiveMessageAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        deadLetteredMessage.ShouldNotBeNull();
    }
    
    public async ValueTask DisposeAsync()
    {
        await _serviceBusMessageProcessor.DisposeAsync();
        await _serviceBusClient.DisposeAsync();
    }

    private async Task DrainSubscriptionAsync(
        ServiceBusReceiverOptions receiverOptions,
        CancellationToken cancellationToken)
    {
        await using var receiver = _serviceBusClient.CreateReceiver(
            TopicName,
            SubscriptionName,
            receiverOptions);

        while (true)
        {
            var message = await receiver.ReceiveMessageAsync(
                TimeSpan.FromMilliseconds(200),
                cancellationToken);

            if (message is null)
                return;

            await receiver.CompleteMessageAsync(message, cancellationToken);
        }
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
                return;

            await Task.Delay(pollInterval, cancellationToken);
        }

        throw new TimeoutException("Condition was not met within the timeout.");
    }
}
