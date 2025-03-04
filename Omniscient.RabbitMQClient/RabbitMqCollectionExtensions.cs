using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Omniscient.RabbitMQClient.Implementations;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient;

public static class RabbitMqCollectionExtensions
{
    public static IServiceCollection AddRabbitMqDependencies(this IServiceCollection services)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"; 
        services.AddEasyNetQ($"host={host}").UseSystemTextJson();
        services.AddHostedService<RabbitMqConsumer>();
        services.AddSingleton<IAsyncPublisher, RabbitMqPublisher>();
        return services;
    }
}