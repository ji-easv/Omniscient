namespace Omniscient.RabbitMQClient.Interfaces;

public interface IMessageHandler<in T>
{
    Task HandleMessage(T message);
}