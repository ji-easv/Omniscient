using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using Omniscient.RabbitMQClient.Implementations;
using Omniscient.RabbitMQClient.Interfaces;

namespace Omniscient.RabbitMQClient;

public static class Dependencies
{
    public static void RegisterRabbitMqDependencies(IServiceCollection services)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
        services.AddEasyNetQ($"host={host}");
        services.AddSingleton<IAsyncConsumer, RabbitMQConsumer>();
    }
}