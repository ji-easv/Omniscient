using EasyNetQ;
using Microsoft.Extensions.Hosting;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Serilog;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqConsumer(IBus bus) : BackgroundService, IAsyncConsumer
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();
    
    private void DiscoverAndRegisterHandlers()
    {
        var handlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRabbitMqMessageHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var messageType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRabbitMqMessageHandler<>))
                .GetGenericArguments()[0];
            Log.Information("Found handler {HandlerTypeName} for message type {MessageTypeName}", handlerType.Name, messageType.Name);
            
            var handlerInstance = Activator.CreateInstance(handlerType);
            if (handlerInstance == null)
            {
                Log.Information("Could not create instance of handler {HandlerTypeName}", handlerType.Name);
                continue;
            }
            
            var methodInfo = handlerType.GetMethod("HandleMessageAsync");
            if (methodInfo == null)
            {
                Log.Information("Could not create instance of handler {HandlerTypeName}", handlerType.Name);
                continue;
            }
            
            // Create a typed delegate for the handler method
            Action<object> typedAction = msg =>
            {
                if (msg is not RabbitMqMessage rabbitMqMessage) return;
                rabbitMqMessage.ExtractPropagatedContext();
                methodInfo.Invoke(handlerInstance, [rabbitMqMessage, CancellationToken.None]);
            };
            
            var subscribeAsyncMethod = typeof(RabbitMqConsumer).GetMethod(nameof(SubscribeAsync))
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