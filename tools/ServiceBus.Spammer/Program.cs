using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string topicName = "topic.1";

const string connectionString =
    "Endpoint=sb://servicebus;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

var activitySource = new ActivitySource("ServiceBus.Spammer");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(r => r.AddService("servicebus-spammer"))
    .AddSource("ServiceBus.Spammer")
    .AddOtlpExporter()
    .Build();

Console.WriteLine("🚀 Service Bus Spammer started...");
Console.WriteLine("Sending 1 message per second to TOPIC 'topic.1'...\n");

await using var client = new ServiceBusClient(connectionString);
var sender = client.CreateSender(topicName);

var counter = 1;

while (true)
{
    try
    {
        var messageBody = $"Message #{counter} @ {DateTime.UtcNow:O}";

        using var activity = activitySource.StartActivity("send message", ActivityKind.Producer);

        var message = new ServiceBusMessage(messageBody)
        {
            ContentType = "text/plain",
            MessageId = Guid.NewGuid().ToString()
        };

        if (activity != null)
        {
            message.ApplicationProperties["traceparent"] =
                $"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-01";
            activity.SetTag("messaging.message_id", message.MessageId);
            activity.SetTag("messaging.system", "servicebus");
        }

        await sender.SendMessageAsync(message);
        Console.WriteLine($"✅ Sent: {messageBody}");

        counter++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }

    await Task.Delay(1000);
}
