using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;

namespace Omniscient.Indexer;

public class EmailMessageHandler : IRabbitMqMessageHandler<EmailMessage>
{
    public Task HandleMessageAsync(EmailMessage message, CancellationToken token = default)
    {
        Console.WriteLine("Received email message: {0}", message); 
        return Task.CompletedTask;
    }
}