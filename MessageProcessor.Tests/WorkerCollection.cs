namespace MessageProcessor.Tests;

[CollectionDefinition("Worker Collection")]
public class WorkerCollection : ICollectionFixture<AzureServiceBusEmulatorFixture>
{
}