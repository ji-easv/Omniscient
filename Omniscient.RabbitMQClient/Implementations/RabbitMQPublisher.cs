using System.Diagnostics;
using EasyNetQ;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;
using Polly.CircuitBreaker;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace Omniscient.RabbitMQClient.Implementations;

public class RabbitMqPublisher(IBus bus, AsyncCircuitBreakerPolicy circuitBreaker) : IAsyncPublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken token = default)
    {
        if (message is not RabbitMqMessage rabbitMqMessage)
        {
            throw new ArgumentException("Message must be of type RabbitMqMessage");
        }
        
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Producer);
        rabbitMqMessage.PropagateContext(activity);
        
        try 
        {
            await circuitBreaker.ExecuteAsync(async () => 
                await bus.PubSub.PublishAsync(message, token));
        }
        catch (BrokenCircuitException ex)
        {
            Log.Error(ex, "Failed to publish - circuit broken");
            // TODO: throw; 
        }
    }
}