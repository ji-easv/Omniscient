using Omniscient.RabbitMQClient.Messages;

namespace Omniscient.RabbitMQClient.Interfaces;

public interface IRabbitMQMessageHandler<in T> : IMessageHandler<T> where T : RabbitMQMessage;