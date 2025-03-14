using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Serilog;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqConsumer : BackgroundService, IAsyncConsumer
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();

    private readonly RabbitMqConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    
    private readonly List<(Type HandlerType, Type MessageType, string Subscription)> _handlerRegistrations = new();
    private bool _handlersDiscovered;

    public RabbitMqConsumer(RabbitMqConnection connection,
        IServiceProvider serviceProvider)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _connection.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private async Task DiscoverAndRegisterHandlers()
    {
        // Only discover handlers once
        if (!_handlersDiscovered)
        {
            // Discover handlers and store in _handlerRegistrations
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
                    
                _handlerRegistrations.Add((handlerType, messageType, messageType.Name));
                Log.Information("Found handler {HandlerTypeName} for message type {MessageTypeName}", 
                    handlerType.Name, messageType.Name);
            }
            
            _handlersDiscovered = true;
        }

        // Establish subscriptions for all discovered handlers
        EstablishSubscriptions();
    }
    
    private void EstablishSubscriptions()
    {
        foreach (var registration in _handlerRegistrations)
        {
            // Skip if subscription already exists
            if (_subscriptions.ContainsKey(registration.Subscription))
                continue;
                
            var handlerInstance = ActivatorUtilities.CreateInstance(_serviceProvider, registration.HandlerType);
            if (handlerInstance == null)
            {
                Log.Information("Could not create instance of handler {HandlerTypeName}", registration.HandlerType.Name);
                continue;
            }

            var methodInfo = registration.HandlerType.GetMethod("HandleMessageAsync");
            if (methodInfo == null)
            {
                Log.Information("Method HandleMessageAsync not found on handler {HandlerTypeName}", 
                    registration.HandlerType.Name);
                continue;
            }

            Action<object> typedAction = msg =>
            {
                if (msg is not RabbitMqMessage rabbitMqMessage) return;
                rabbitMqMessage.ExtractPropagatedContext();

                // This is the key change: invoke the async method and wait for it synchronously
                var task = (Task)methodInfo.Invoke(handlerInstance, [rabbitMqMessage, CancellationToken.None]);

                // Block until the task completes - this ensures sequential processing
                task.GetAwaiter().GetResult();
            };

            var subscribeAsyncMethod = typeof(RabbitMqConsumer).GetMethod(nameof(SubscribeAsync))
                .MakeGenericMethod(registration.MessageType);

            subscribeAsyncMethod.Invoke(this, [registration.Subscription, typedAction, CancellationToken.None]);
        }
    }

    public async Task SubscribeAsync<T>(string subscription, Action<T> handler,
        CancellationToken cancellationToken = default)
    {
        if (_subscriptions.ContainsKey(subscription))
        {
            throw new ArgumentException("Subscription already exists");
        }

        var subscriptionHandle = await _connection.Bus.PubSub.SubscribeAsync<T>(
            subscription,
            msg =>
            {
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
        );

        _subscriptions.Add(subscription, subscriptionHandle);
        Log.Information("Subscribed to {MessageType}", typeof(T).Name);
    }

    public Task UnsubscribeAsync(string subscription, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryGetValue(subscription, out IDisposable? subscriptionHandle))
        {
            throw new ArgumentException("Subscription does not exist");
        }

        subscriptionHandle.Dispose();
        _subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DiscoverAndRegisterHandlers();
    }
    
    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        if (isConnected)
        {
            EstablishSubscriptions();
        }
        else
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Value.Dispose();
            }
            _subscriptions.Clear();
        }
    }
}