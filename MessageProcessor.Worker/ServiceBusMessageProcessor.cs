namespace MessageProcessor.Worker;

using Azure.Messaging.ServiceBus;

public class ServiceBusMessageProcessor : IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusProcessor _processor;
    private readonly IMyGitHubApiClient _apiClient;

    public ServiceBusMessageProcessor(
        ServiceBusClient serviceBusClient, 
        string topicName, 
        string subscriptionName,
        IMyGitHubApiClient apiClient)
    {
        _serviceBusClient = serviceBusClient;

        _processor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;
        
        _apiClient = apiClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _processor.StartProcessingAsync(cancellationToken);
        Console.WriteLine("Service Bus message processor started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        Console.WriteLine("Service Bus message processor stopped.");
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            // TODO: Add your business logic here
            Console.WriteLine("Calculating the meaning of life... please wait.");
            var result = await _apiClient.GetRootAsync();
            Console.WriteLine(result);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in HandleMessageAsync: {ex.Message}\n{ex.StackTrace}");
            await args.DeadLetterMessageAsync(args.Message);
        }
    }

    private static Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine(
            $"Error in the Service Bus processor: {args.Exception.Message}\nErrorSource: {args.ErrorSource}\nEntity Path: {args.EntityPath}\nNamespace: {args.FullyQualifiedNamespace}");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _processor.DisposeAsync();
        await _serviceBusClient.DisposeAsync();
    }
}