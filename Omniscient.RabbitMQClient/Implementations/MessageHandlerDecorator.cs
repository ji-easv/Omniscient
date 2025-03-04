using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;

namespace Omniscient.RabbitMQClient.Implementations;

public class MessageHandlerDecorator<T>(IMessageHandler<T> innerHandler) : IMessageHandler<T>
    where T : RabbitMqMessage
{
    public async Task HandleMessageAsync(T message, CancellationToken token = default)
    {
        message.ExtractPropagatedContext();
        await innerHandler.HandleMessageAsync(message, token);
    }
}