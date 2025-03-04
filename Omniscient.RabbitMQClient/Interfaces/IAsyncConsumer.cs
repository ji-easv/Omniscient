namespace Omniscient.RabbitMQClient.Interfaces;

public interface IAsyncConsumer
{
    void RegisterHandler<T>(string subscription, Func<T, Task> handler);
}