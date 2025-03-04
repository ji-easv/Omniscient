using System.Diagnostics;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;

namespace Omniscient.Indexer;

public class EmailMessageHandler : IRabbitMqMessageHandler<EmailMessage>
{
    public Task HandleMessageAsync(EmailMessage message, CancellationToken token = default)
    {
        using var activity = Monitoring.ActivitySource.StartActivity(ActivityKind.Consumer, message.ActivityContext);
        Console.WriteLine("Received email message: {0}", message); 
        return Task.CompletedTask;
    }
}