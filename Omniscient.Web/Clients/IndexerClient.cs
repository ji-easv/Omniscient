using Omniscient.Shared;
using Omniscient.Shared.Dtos;

namespace Omniscient.Web.Clients;

public class IndexerClient(HttpClient httpClient, ILogger<IndexerClient> logger)
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
    
    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        logger.LogInformation("SearchEmailsAsync: {query}", query);
        
        using var cts = new CancellationTokenSource(_timeout);
        var response = await httpClient.GetAsync(
            $"api/indexer?query={query}&pageNumber={pageNumber}&pageSize={pageSize}", 
            cts.Token);
            
        logger.LogInformation("SearchEmailsAsync Response: {response}", response);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>(
            cancellationToken: cts.Token) ?? PaginatedList<EmailDto>.Empty();
    }

    public async Task<string> GetFullContentAsync(Guid id)
    {
        logger.LogInformation("GetFullContentAsync: {id}", id);
        
        using var cts = new CancellationTokenSource(_timeout);
        var response = await httpClient.GetAsync(
            $"api/indexer/full-content/{id}", 
            cts.Token);
            
        logger.LogInformation("GetFullContentAsync Response: {response}", response);
        response.EnsureSuccessStatusCode();
        
        // Using ReadAsStringAsync instead of ReadFromJsonAsync for plain text
        return await response.Content.ReadAsStringAsync(cts.Token);
    }
}