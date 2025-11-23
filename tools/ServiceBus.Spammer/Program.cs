using Azure.Messaging.ServiceBus;

const string topicName = "topic.1";

const string connectionString =
    "Endpoint=sb://servicebus;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

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

        var message = new ServiceBusMessage(messageBody)
        {
            ContentType = "text/plain",
            MessageId = Guid.NewGuid().ToString()
        };

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
