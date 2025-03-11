using System.Net.Sockets;
using EasyNetQ;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Omniscient.RabbitMQClient.Implementations;
using Omniscient.RabbitMQClient.Interfaces;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace Omniscient.RabbitMQClient;

public static class RabbitMqCollectionExtensions
{
    public static IServiceCollection AddRabbitMqDependencies(this IServiceCollection services)
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost"; 
        
        services.AddSingleton<AsyncCircuitBreakerPolicy>(sp => {
            return Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .Or<EasyNetQException>()
                .Or<ConnectFailureException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (ex, time) => Log.Warning("RabbitMQ circuit opened: {Message}", ex.Message),
                    onReset: () => Log.Information("RabbitMQ circuit closed"),
                    onHalfOpen: () => Log.Information("RabbitMQ circuit half-open")
                );
        });

        services.AddEasyNetQ($"host={host}")
            .UseSystemTextJson();
        
        services.AddHostedService<RabbitMqConsumer>();
        services.AddSingleton<IAsyncPublisher, RabbitMqPublisher>();
        return services;
    }
}