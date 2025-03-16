using Omniscient.Shared;
using Omniscient.Shared.Dtos;

namespace Omniscient.Web.Clients;

public class IndexerClient(IHttpClientFactory httpClientFactory, ILogger<IndexerClient> logger)
{
    private readonly HttpClient _shard1Client = httpClientFactory.CreateClient("Shard1IndexerClient");
    private readonly HttpClient _shard2Client = httpClientFactory.CreateClient("Shard2IndexerClient");
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        if (pageSize % 2 != 0)
            throw new ArgumentException("PageSize must be an even number.");
        
        logger.LogInformation("SearchEmailsAsync: {query}", query);
        
        using var cts = new CancellationTokenSource(_timeout);
        var shard1Task = _shard1Client.GetAsync(
            $"api/indexer?query={query}&pageIndex={pageNumber}&pageSize={pageSize / 2}",
            cts.Token);
        var shard2Task = _shard2Client.GetAsync(
            $"api/indexer?query={query}&pageIndex={pageNumber}&pageSize={pageSize / 2}",
            cts.Token);
        
        await Task.WhenAll(shard1Task, shard2Task);

        var shard1Response = await shard1Task;
        var shard2Response = await shard2Task;

        shard1Response.EnsureSuccessStatusCode();
        shard2Response.EnsureSuccessStatusCode();

        var shard1Emails = await shard1Response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>(cancellationToken: cts.Token) ?? PaginatedList<EmailDto>.Empty();
        var shard2Emails = await shard2Response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>(cancellationToken: cts.Token) ?? PaginatedList<EmailDto>.Empty();
        
        var interleavedEmails = shard1Emails.Items.Zip(shard2Emails.Items, (s1, s2) => new[] { s1, s2 })
            .SelectMany(x => x)
            .ToList();
        
        return new PaginatedList<EmailDto>(interleavedEmails, shard1Emails.TotalCount + shard2Emails.TotalCount, pageNumber, pageSize);
    }

    public async Task<string> GetFullContentAsync(Guid id)
    {
        logger.LogInformation("GetFullContentAsync: {id}", id);

        using var cts = new CancellationTokenSource(_timeout);
        var shard1Response = await _shard1Client.GetAsync(
            $"api/indexer/full-content/{id}",
            cts.Token);

        if (shard1Response.IsSuccessStatusCode)
        {
            logger.LogInformation("GetFullContentAsync Response from Shard1: {response}", shard1Response);
            var content = await shard1Response.Content.ReadAsStringAsync(cts.Token);
            return content;
        }

        var shard2Response = await _shard2Client.GetAsync(
            $"api/indexer/full-content/{id}",
            cts.Token);

        if (shard2Response.IsSuccessStatusCode)
        {
            logger.LogInformation("GetFullContentAsync Response from Shard2: {response}", shard2Response);
            var content = await shard2Response.Content.ReadAsStringAsync(cts.Token);
            return content;
        }

        throw new HttpRequestException("Failed to get full content from both shards.");
    }
}