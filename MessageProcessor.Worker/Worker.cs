namespace MessageProcessor.Worker;

public class Worker(ServiceBusMessageProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await processor.StartAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected during graceful shutdown, no action needed
        }

        await processor.StopAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}