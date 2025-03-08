using System.Diagnostics;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;
using Serilog;

namespace Omniscient.Indexer;

public class EmailMessageHandler : IRabbitMqMessageHandler<EmailMessage>
{
    public Task HandleMessageAsync(EmailMessage message, CancellationToken token = default)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Consumer, message.ActivityContext);
        Log.Information("Received email message: {@Message}", message);
        return Task.CompletedTask;
    }
}