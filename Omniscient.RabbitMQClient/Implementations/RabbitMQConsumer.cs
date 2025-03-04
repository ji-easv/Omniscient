using EasyNetQ;
using Microsoft.Extensions.Hosting;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMQConsumer(IBus bus) : BackgroundService, IAsyncConsumer
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();
    
    private void DiscoverAndRegisterHandlers()
    {
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRabbitMQMessageHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var messageType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRabbitMQMessageHandler<>))
                .GetGenericArguments()[0];
            Console.WriteLine($"Found handler {handlerType.Name} for message type {messageType.Name}");
            
            var handlerInstance = Activator.CreateInstance(handlerType);
            if (handlerInstance == null)
            {
                Console.WriteLine($"Could not create instance of handler {handlerType.Name}");
                continue;
            }
            
            var methodInfo = handlerType.GetMethod("HandleMessageAsync");
            if (methodInfo == null)
            {
                Console.WriteLine($"Could not find HandleMessageAsync method in handler {handlerType.Name}");
                continue;
            }
            
            // Create a typed delegate for the handler method
            Action<object> typedAction = msg =>
            {
                methodInfo.Invoke(handlerInstance, [msg, CancellationToken.None]);
            };
            
            var subscribeAsyncMethod = typeof(RabbitMQConsumer).GetMethod(nameof(SubscribeAsync))
                .MakeGenericMethod(messageType);

            subscribeAsyncMethod.Invoke(this, [messageType.Name, typedAction, CancellationToken.None]);
        }
    }

    public async Task SubscribeAsync<T>(string subscription, Action<T> handler, CancellationToken cancellationToken = default)
    {
        if(_subscriptions.ContainsKey(subscription))
        {
            throw new ArgumentException("Subscription already exists");
        }
        var subscriptionHandle = await bus.PubSub.SubscribeAsync<T>(
            subscription,
            handler,
            cancellationToken
        );
        _subscriptions.Add(subscription, subscriptionHandle);
    }

    public Task UnsubscribeAsync(string subscription, CancellationToken cancellationToken = default)
    {
        if(!_subscriptions.TryGetValue(subscription, out IDisposable? subscriptionHandle))
        {
            throw new ArgumentException("Subscription does not exist");
        }

        subscriptionHandle.Dispose();
        _subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DiscoverAndRegisterHandlers();
    }
}