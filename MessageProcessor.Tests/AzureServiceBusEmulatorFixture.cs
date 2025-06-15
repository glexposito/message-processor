using DotNet.Testcontainers.Builders;
using Testcontainers.ServiceBus;

namespace MessageProcessor.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class AzureServiceBusEmulatorFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer;
    private const ushort ServiceBusPort = 5672;
    private const ushort ServiceBusHttpPort = 5300;

    public AzureServiceBusEmulatorFixture()
    {
        _serviceBusContainer = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true)
            .WithPortBinding(ServiceBusPort, true)
            .WithPortBinding(ServiceBusHttpPort, true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            // ReSharper disable once StringLiteralTypo
            .WithName("azure-servicebus-emulator")
            .WithLabel("purpose", "integration-test")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(req =>
                        req.ForPort(ServiceBusHttpPort).ForPath("/health")))
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _serviceBusContainer.GetConnectionString();

    public async ValueTask InitializeAsync() => await _serviceBusContainer
        .StartAsync()
        .ConfigureAwait(false);

    public async ValueTask DisposeAsync()
    {
        await _serviceBusContainer
            .DisposeAsync()
            .ConfigureAwait(false);

        GC.SuppressFinalize(this);
    } // Emulator Service is Successfully Up!
}