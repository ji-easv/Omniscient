using Omniscient.Shared.Entities;

namespace Omniscient.RabbitMQClient.Messages;

public class EmailMessage : RabbitMqMessage
{
    public required string Sender { get; set; }
    public List<Email> Emails { get; set; }
}