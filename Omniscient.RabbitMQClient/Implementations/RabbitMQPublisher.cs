using System.Diagnostics;
using EasyNetQ;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqPublisher(IBus bus) : IAsyncPublisher 
{
    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        if (message is not RabbitMqMessage rabbitMqMessage)
        {
            throw new ArgumentException("Message must be of type RabbitMqMessage");
        }
        
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Producer);
        rabbitMqMessage.PropagateContext(activity);
        await bus.PubSub.PublishAsync(message, token);
    }
}