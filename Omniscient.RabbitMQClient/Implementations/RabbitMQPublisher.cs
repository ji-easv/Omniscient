using System.Diagnostics;
using EasyNetQ;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqPublisher(IBus bus) : IAsyncPublisher 
{
    public Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        using var activity = new Activity("PublishAsync");
        if (message is not RabbitMqMessage rabbitMqMessage)
        {
            throw new ArgumentException("Message must be of type RabbitMqMessage");
        }
        
        rabbitMqMessage.PropagateContext(activity);
        return bus.PubSub.PublishAsync(message, token);
    }
}