using System.Diagnostics;
using Omniscient.Indexer.Domain.Services;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;

namespace Omniscient.Indexer;

public class EmailMessageHandler(IServiceProvider serviceProvider) : IRabbitMqMessageHandler<EmailMessage>
{
    public async Task HandleMessageAsync(EmailMessage message, CancellationToken token = default)
    {
        ShouldProcessMessage(message);

        using var activity = ActivitySources.OmniscientActivitySource.StartActivity(ActivityKind.Consumer, message.ActivityContext);
        using var scope = serviceProvider.CreateScope();
        
        var indexerService = scope.ServiceProvider.GetRequiredService<IIndexerService>();
        //Log.Information("Received email message: {@Message}", message);
        await indexerService.IndexEmails(message.Emails);
    }
    
    private bool ShouldProcessMessage(EmailMessage message)
    {
        if (string.IsNullOrEmpty(message.Sender))
            throw new Exception("Do not process message!");

        char firstChar = char.ToUpper(message.Sender[0]);
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var start = EnvironmentHelper.GetValue("SENDER_START", configuration);
        var end = EnvironmentHelper.GetValue("SENDER_END", configuration);
        
        if(firstChar >= start[0] && firstChar <= end[0])
            return true;

        throw new Exception("Do not process message!");
    }

}