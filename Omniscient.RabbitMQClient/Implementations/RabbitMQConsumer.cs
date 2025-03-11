using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Polly.CircuitBreaker;
using Serilog;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqConsumer(
    IBus bus,
    IServiceProvider serviceProvider,
    AsyncCircuitBreakerPolicy circuitBreakerPolicy)
    : BackgroundService, IAsyncConsumer
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();
    private readonly Dictionary<string, (Type MessageType, object Handler)> _handlerRegistry = new();
    
    private Timer? _reconnectionTimer;

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
            
            var handlerInstance = ActivatorUtilities.CreateInstance(serviceProvider, handlerType);
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
            
            // Store in registry for reconnection purposes
            _handlerRegistry[messageType.Name] = (messageType, typedAction);
            
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
        
        try
        {
            var subscriptionHandle = await circuitBreakerPolicy.ExecuteAsync(async () =>
                await bus.PubSub.SubscribeAsync<T>(
                    subscription,
                    msg => {
                        try
                        {
                            handler(msg);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error processing message: {MessageType}", typeof(T).Name);
                        }
                    },
                    cancellationToken
                ));
                
            _subscriptions.Add(subscription, subscriptionHandle);
            Log.Information("Subscribed to {MessageType}", typeof(T).Name);
        }
        catch (BrokenCircuitException ex)
        {
            Log.Error(ex, "Failed to subscribe - circuit broken");
        }
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DiscoverAndRegisterHandlers();
        
        // Monitor circuit status and subscriptions
        _reconnectionTimer = new Timer(_ => 
        {
            if (circuitBreakerPolicy.CircuitState != CircuitState.Open && 
                _subscriptions.Count < _handlerRegistry.Count)
            {
                Log.Information("Attempting to restore subscriptions");
                RestoreSubscriptions();
            }
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }
    
    private void RestoreSubscriptions()
    {
        foreach (var (subscription, (messageType, handler)) in _handlerRegistry)
        {
            if (_subscriptions.ContainsKey(subscription)) continue;
            
            var subscribeMethod = typeof(RabbitMqConsumer).GetMethod(nameof(SubscribeAsync))
                .MakeGenericMethod(messageType);
            
            try
            {
                subscribeMethod.Invoke(this, [subscription, handler, CancellationToken.None]);
                Log.Information("Restored subscription: {Subscription}", subscription);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restore subscription: {Subscription}", subscription);
            }
        }
    }
    
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _reconnectionTimer?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}