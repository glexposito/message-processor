namespace MessageProcessor.Worker;

public class Worker(ServiceBusMessageProcessor processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await processor.StartAsync(stoppingToken);  // startup failure should bubble

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // expected during shutdown
        }
        finally
        {
            using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await processor.StopAsync(shutdownCts.Token);
            await processor.DisposeAsync();
        }
    }
}
