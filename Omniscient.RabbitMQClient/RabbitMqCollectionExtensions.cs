using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Omniscient.RabbitMQClient.Implementations;

namespace Omniscient.RabbitMQClient;

public static class RabbitMqCollectionExtensions
{
    public static IServiceCollection AddRabbitMqDependencies(this IServiceCollection services)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"; 
        services.AddEasyNetQ($"host={host}").UseSystemTextJson();
        services.AddHostedService<RabbitMQConsumer>();
        return services;
    }
}