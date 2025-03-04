namespace Omniscient.RabbitMQClient.Interfaces;

public interface IAsyncPublisher
{
    Task PublishAsync<T>(T message, CancellationToken token = default);
}