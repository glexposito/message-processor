using Azure.Messaging.ServiceBus;

namespace MessageProcessor.Tests;

using Testcontainers.ServiceBus;
using Xunit;

public class ServiceBusIntegrationTests : IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer = new ServiceBusBuilder()
        .WithAcceptLicenseAgreement(true)
        //.WithResourceMapping("Config.json", "/ServiceBus_Emulator/ConfigFiles/")
        .Build();

    private string ConnectionString => _serviceBusContainer.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _serviceBusContainer
            .StartAsync()
            .ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusContainer
            .DisposeAsync()
            .ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CanSendAndReceiveMessage()
    {
        // Arrange
        await using var client = new ServiceBusClient(ConnectionString);
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