namespace MessageProcessor.Worker;

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

public class ServiceBusMessageProcessor : IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusProcessor _processor;
    private readonly IGreetingsClient _greetingsClient;
    private readonly ILogger<ServiceBusMessageProcessor> _logger;

    public ServiceBusMessageProcessor(
        ServiceBusClient serviceBusClient,
        string topicName,
        string subscriptionName,
        IGreetingsClient greetingsClient,
        ILogger<ServiceBusMessageProcessor> logger)
    {
        _serviceBusClient = serviceBusClient;

        _processor = _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        _greetingsClient = greetingsClient;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Service Bus message processor started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("Service Bus message processor stopped.");
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            // TODO: Add your business logic here
            _logger.LogInformation("Calculating the meaning of life for Message ID {MessageId}... please wait.", args.Message.MessageId);
            _logger.LogInformation("Received: {Body}", args.Message.Body);
            var result = await _greetingsClient.SayHelloAsync();
            _logger.LogInformation("{Result}", result);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in HandleMessageAsync for Message ID {MessageId}.", args.Message.MessageId);
            await args.DeadLetterMessageAsync(args.Message);
        }
    }

    private Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Error in the Service Bus processor. ErrorSource: {ErrorSource}, Entity Path: {EntityPath}, Namespace: {Namespace}",
            args.ErrorSource,
            args.EntityPath,
            args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _processor.DisposeAsync();
        await _serviceBusClient.DisposeAsync();
    }
}
