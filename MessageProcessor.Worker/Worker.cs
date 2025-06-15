namespace MessageProcessor.Worker;

public class Worker : BackgroundService
{
    private readonly ServiceBusMessageProcessor _processor = new(
        connectionString: "<your-connection-string>",
        topicName: "your-topic",
        subscriptionName: "your-subscription");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _processor.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected during graceful shutdown, no action needed
        }

        await _processor.StopAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}