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
        services.AddEasyNetQ(cfg =>
            {
                cfg.Hosts.Add(new HostConfiguration(host, 5672));
                cfg.PrefetchCount = 1;
            })
            .UseSystemTextJson();
        services.AddHostedService<RabbitMqConsumer>();
        services.AddSingleton<IAsyncPublisher, RabbitMqPublisher>();
        return services;
    }
}