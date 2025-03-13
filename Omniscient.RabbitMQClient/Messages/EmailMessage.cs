using Omniscient.Shared.Entities;

namespace Omniscient.RabbitMQClient.Messages;

public class EmailMessage : RabbitMqMessage
{
    public List<Email> Emails { get; set; }
}