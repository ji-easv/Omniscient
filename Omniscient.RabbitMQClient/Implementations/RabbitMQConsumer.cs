using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EasyNetQ;
using Microsoft.Extensions.Hosting;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMQConsumer : BackgroundService, IAsyncConsumer
{
    private readonly ConcurrentDictionary<string, (Type MessageType, Func<object, Task> Handler)>
        _handlers = new();

    private readonly IBus _bus;

    public RabbitMQConsumer(IBus bus)
    {
        _bus = bus;
        DiscoverAndRegisterHandlers();
    }

    public void RegisterHandler<T>(string subscription, Func<T, Task> handler)
    {
        Console.WriteLine("Registering handler for subscription: " + subscription);
        if (!_handlers.TryAdd(subscription, (typeof(T), async (message) => await handler((T)message))))
        {
            throw new ArgumentException("Subscription already established.");
        }
    }

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

            var subscription = messageType.Name;
            var handlerInstance = Activator.CreateInstance(handlerType);
            var methodInfo = handlerType.GetMethod("HandleAsync");

            var registerHandlerMethod = typeof(RabbitMQConsumer).GetMethod(nameof(RegisterHandler))
                .MakeGenericMethod(messageType);

            registerHandlerMethod.Invoke(this,
                new object[]
                {
                    subscription,
                    new Func<object, Task>(async (message) =>
                    {
                        await (Task)methodInfo.Invoke(handlerInstance, new[] { message });
                    })
                });
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var subscription in _handlers.Keys)
        {
            Console.WriteLine("Subscribing to: " + subscription);
            var (messageType, handler) = _handlers[subscription];
            var subscribeMethod = typeof(IPubSub).GetMethod("SubscribeAsync");

            var genericSubscribeMethod = subscribeMethod.MakeGenericMethod(messageType);
            Func<object, CancellationToken, Task> typedFunc = (msg, token) =>
            {
                return handler(msg);
            };

            // SubscribeAsync<T> expects a `Action<T>`
            // Using a reflection "trick" to create a lambda expression
            var param = Expression.Parameter(messageType, "message");
            var tokenParam = Expression.Parameter(typeof(CancellationToken), "token");
            var expr = Expression.Invoke(Expression.Constant(typedFunc), param, tokenParam);
            var lambdaType = typeof(Func<,,>).MakeGenericType(messageType, typeof(CancellationToken), typeof(Task));
            var lambda = Expression.Lambda(lambdaType, expr, param, tokenParam).Compile();

            var subscriptionId = $"{messageType.Name}";

            genericSubscribeMethod.Invoke(
                obj: _bus.PubSub,
                parameters: new object[]
                {
                    subscriptionId, lambda, new Action<ISubscriptionConfiguration>(_ => { }), CancellationToken.None
                });
        }
    }
}