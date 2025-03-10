using System.Diagnostics;
using Omniscient.Indexer.Domain.Services;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;
using Serilog;

namespace Omniscient.Indexer;

public class EmailMessageHandler(IServiceProvider serviceProvider) : IRabbitMqMessageHandler<EmailMessage>
{
    public async Task HandleMessageAsync(EmailMessage message, CancellationToken token = default)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Consumer, message.ActivityContext);
        using var scope = serviceProvider.CreateScope();
        
        var indexerService = scope.ServiceProvider.GetRequiredService<IIndexerService>();
        Log.Information("Received email message: {@Message}", message);
        await indexerService.IndexEmail(message.Email);
    }
}