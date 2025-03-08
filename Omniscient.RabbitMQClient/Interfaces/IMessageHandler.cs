namespace Omniscient.RabbitMQClient.Interfaces;

public interface IMessageHandler<in T>
{
    Task HandleMessageAsync(T message, CancellationToken token = default);
}