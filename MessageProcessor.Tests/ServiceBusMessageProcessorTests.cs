using Azure.Messaging.ServiceBus;
using MessageProcessor.Worker;
using Shouldly;
using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace MessageProcessor.Tests;

[Collection("Worker Collection")]
public class ServiceBusMessageProcessorTests
{
    private const string TopicName = "topic.1";
    private const string SubscriptionName = "subscription.3";
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusMessageProcessor _serviceBusMessageProcessor;
    private readonly WireMockServer _wireMockServer;

    public ServiceBusMessageProcessorTests(AzureServiceBusEmulatorFixture fixture)
    {
        _wireMockServer = fixture.WireMockServer;
        var wireMockUrl = _wireMockServer.Url;
        var httpClient = new HttpClient { BaseAddress = new Uri(wireMockUrl!) };
        IMyGitHubApiClient apiClient = new MyGitHubApiClient(httpClient);

        _serviceBusClient = new ServiceBusClient(fixture.ConnectionString);
        _serviceBusMessageProcessor =
            new ServiceBusMessageProcessor(_serviceBusClient, TopicName, SubscriptionName, apiClient);
    }

    [Fact]
    public async Task Processor_RemovesMessageFromSubscription_WhenProcessingSucceeds()
    {
        // Arrange
        var sender = _serviceBusClient.CreateSender(TopicName);
        await sender.SendMessageAsync(
            new ServiceBusMessage("message"),
            TestContext.Current.CancellationToken);

        _wireMockServer.Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("mocked response"));

        var receiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName);

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);

        // Wait until no messages remain in the subscription
        await WaitUntilAsync(
            async () =>
            {
                var nextMessage =
                    await receiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
                return nextMessage == null;
            },
            timeout: TimeSpan.FromSeconds(10),
            pollInterval: TimeSpan.FromMilliseconds(50),
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        var remainingMessage = await receiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        remainingMessage.ShouldBeNull();
    }


    [Fact]
    public async Task Processor_MovesMessageToDeadLetterQueue_WhenProcessingFails()
    {
        // Arrange
        _wireMockServer.Given(Request.Create().WithPath("/").UsingGet())
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