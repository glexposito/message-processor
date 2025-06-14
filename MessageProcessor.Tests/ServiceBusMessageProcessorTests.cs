using Azure.Messaging.ServiceBus;
using MessageProcessor.Worker;
using Shouldly;

namespace MessageProcessor.Tests;

[Collection("Worker Collection")]
public class ServiceBusMessageProcessorTests
{
    private const string TopicName = "topic.1";
    private const string SubscriptionName = "subscription.3";
    private readonly ServiceBusClient _client;
    private readonly ServiceBusMessageProcessor _serviceBusMessageProcessor;

    public ServiceBusMessageProcessorTests(AzureServiceBusEmulatorFixture fixture)
    {
        _client = new ServiceBusClient(fixture.ConnectionString);
        _serviceBusMessageProcessor = new ServiceBusMessageProcessor(_client, TopicName, SubscriptionName);
    }
    
    [Fact]
    public async Task StartAsync_ShouldProcessAndRemoveMessageFromSubscription_WhenProcessorIsStartedAndRun()
    {
        // Arrange
        var sender = _client.CreateSender(TopicName);
        var message = new ServiceBusMessage("prepopulated message");
        await sender.SendMessageAsync(message, TestContext.Current.CancellationToken);
        
        var receiver = _client.CreateReceiver(TopicName, SubscriptionName);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var peeked = await receiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        peeked.ShouldNotBeNull(); // Ensures at least one message is present

        // Act
        await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);

        // Assert: Ensure no more messages remain
        var afterProcessorReceiver = _client.CreateReceiver(TopicName, SubscriptionName);
        var afterProcessorPeek = await afterProcessorReceiver.PeekMessageAsync(1, TestContext.Current.CancellationToken);
        afterProcessorPeek.ShouldBeNull(); // There should be no messages left
    }
}