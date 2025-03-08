using Omniscient.Shared.Entities;

namespace Omniscient.RabbitMQClient.Messages;

public class EmailMessage : RabbitMqMessage
{
    public Email Email { get; set; }
}