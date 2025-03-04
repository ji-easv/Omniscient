using EasyNetQ;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMQConsumer(IBus bus) : IAsyncConsumer
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();

    public async Task SubscribeAsync<T>(string subscription, Action<T> handler, CancellationToken cancellationToken = default)
    {
        if(_subscriptions.ContainsKey(subscription))
        {
            throw new ArgumentException("Subscription already established.");
        }
        
        var subscriptionHandle = await bus.PubSub.SubscribeAsync<T>(
            subscription,
            handler,
            cancellationToken
        );
        _subscriptions.Add(subscription, subscriptionHandle);
    }
}