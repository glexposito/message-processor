namespace MessageProcessor.Worker;

using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

public class ServiceBusMessageProcessor : IAsyncDisposable
{
    private static readonly ActivitySource ActivitySource = new("MessageProcessor.Worker");

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
        ActivityContext parentContext = default;
        if (args.Message.ApplicationProperties.TryGetValue("traceparent", out var traceparentObj)
            && traceparentObj is string traceparent)
        {
            ActivityContext.TryParse(traceparent, null, isRemote: true, out parentContext);
        }

        using var activity = ActivitySource.StartActivity("process message", ActivityKind.Consumer, parentContext);
        activity?.SetTag("messaging.message_id", args.Message.MessageId);
        activity?.SetTag("messaging.system", "servicebus");

        try
        {
            // TODO: Add your business logic here
            _logger.LogInformation("Calculating the meaning of life for Message ID {MessageId}... please wait.", args.Message.MessageId);
            _logger.LogInformation("Received: {Body}", args.Message.Body);
            var result = await _greetingsClient.SayHelloAsync();
            _logger.LogInformation("Greeting result: {Result}", result);

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
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
