using Omniscient.RabbitMQClient.Messages;

namespace Omniscient.RabbitMQClient.Interfaces;

public interface IRabbitMqMessageHandler<in T> : IMessageHandler<T> where T : RabbitMqMessage;