using EasyNetQ;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqPublisher(IBus bus) : IAsyncPublisher 
{
    public Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        return bus.PubSub.PublishAsync(message, token);
    }
}