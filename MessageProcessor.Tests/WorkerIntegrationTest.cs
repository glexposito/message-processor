using Azure.Messaging.ServiceBus;

namespace MessageProcessor.Tests;

[Collection("Worker Collection")]
public class WorkerIntegrationTest(AzureServiceBusEmulatorFixture fixture)
{
    [Fact]
    public async Task CanSendAndReceiveMessage()
    {
        // Arrange
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        var sender = client.CreateSender("queue.1");
        var receiver = client.CreateReceiver("queue.1");

        // Act
        await sender.SendMessageAsync(new ServiceBusMessage("Hello, Service Bus!"),
            TestContext.Current.CancellationToken);

        // Assert
        var received =
            await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.NotNull(received);
        Assert.Equal("Hello, Service Bus!", received.Body.ToString());
    }
}