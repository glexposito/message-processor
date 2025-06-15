using Azure.Messaging.ServiceBus;
using MessageProcessor.Worker;
using Shouldly;

namespace MessageProcessor.Tests;

[Collection("Worker Collection")]
public class ServiceBusMessageProcessorTests
{
    private const string TopicName = "topic.1";
    private const string SubscriptionName = "subscription.1";
    private readonly ServiceBusClient _client;
    private readonly ServiceBusMessageProcessor _serviceBusMessageProcessor;

    public ServiceBusMessageProcessorTests(AzureServiceBusEmulatorFixture fixture)
    {
        _client = new ServiceBusClient(fixture.ConnectionString);
        _serviceBusMessageProcessor = new ServiceBusMessageProcessor(_client, TopicName, SubscriptionName);
    }
    
    [Fact]
    public async Task StartAsync_ShouldProcessMessageSuccessfully()
    {
        // Arrange
        var sender = _client.CreateSender(TopicName);
        var message = new ServiceBusMessage("prepopulated message");
        await sender.SendMessageAsync(message, TestContext.Current.CancellationToken);

        // Act
        // await _serviceBusMessageProcessor.StartAsync(TestContext.Current.CancellationToken);

        // Wait a bit for the processor to process the message
        await Task.Delay(2000, TestContext.Current.CancellationToken);

        // Assert
        var receiver = _client.CreateReceiver(TopicName, SubscriptionName);
        var received = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        received.ShouldBeNull();

        await _serviceBusMessageProcessor.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ProcessesMessageSuccessfully()
    {
        // Arrange
        var sender = _client.CreateSender(TopicName);
        var message = new ServiceBusMessage("integration test message");
        await sender.SendMessageAsync(message, TestContext.Current.CancellationToken);

        // Act
        var receiver = _client.CreateReceiver(TopicName, SubscriptionName);
        var received =
            await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Assert
        received.ShouldBeNull();
    }
}