using EasyNetQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Omniscient.RabbitMQClient.Implementations;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.ServiceDefaults;

namespace Omniscient.RabbitMQClient;

public static class RabbitMqCollectionExtensions
{
    public static WebApplicationBuilder AddRabbitMqDependencies(this WebApplicationBuilder builder)
    {
        var host = EnvironmentHelper.GetValue("RABBITMQ_HOST", builder.Configuration); 
        builder.Services.AddEasyNetQ(cfg =>
            {
                cfg.Hosts.Add(new HostConfiguration(host, 5672));
                cfg.PrefetchCount = 1;
            })
            .UseSystemTextJson();
        builder.Services.AddHostedService<RabbitMqConsumer>();
        builder.Services.AddSingleton<IAsyncPublisher, RabbitMqPublisher>();
        return builder;
    }
}