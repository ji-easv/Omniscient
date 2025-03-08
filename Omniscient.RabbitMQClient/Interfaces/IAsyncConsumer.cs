namespace Omniscient.RabbitMQClient.Interfaces;

public interface IAsyncConsumer
{
    Task SubscribeAsync<T>(string subscription, Action<T> handler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string subscription, CancellationToken cancellationToken = default);
}