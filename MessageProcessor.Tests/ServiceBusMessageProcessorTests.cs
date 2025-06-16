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
    public async Task StartAsync_ShouldRemoveMessage_WhenProcessingRunsSuccessfully()
    {
        // Arrange
        var sender = _serviceBusClient.CreateSender(TopicName);
        var message = new ServiceBusMessage("prepopulated message");
        await sender.SendMessageAsync(message, TestContext.Current.CancellationToken);

        _wireMockServer.Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("mocked response"));

        var receiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var peeked = await receiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        peeked.ShouldNotBeNull(); // Ensures at least one message is present

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert: Ensure no more messages remain
        var afterProcessorReceiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName);
        var afterProcessorPeek =
            await afterProcessorReceiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        afterProcessorPeek.ShouldBeNull(); // There should be no messages left
    }

    [Fact]
    public async Task StartAsync_ShouldMoveMessageToDlq_WhenProcessingFails()
    {
        // Arrange
        _wireMockServer.Given(Request.Create().WithPath("/").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var sender = _serviceBusClient.CreateSender(TopicName);
        var message = new ServiceBusMessage("will fail");
        await sender.SendMessageAsync(message, TestContext.Current.CancellationToken);

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert: Check DLQ
        var dlqReceiver = _serviceBusClient.CreateReceiver(TopicName, SubscriptionName, new ServiceBusReceiverOptions
        {
            SubQueue = SubQueue.DeadLetter
        });
        var dlqMessage = await dlqReceiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        dlqMessage.ShouldNotBeNull();
    }
}