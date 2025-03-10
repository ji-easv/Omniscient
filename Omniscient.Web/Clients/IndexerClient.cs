using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;

namespace Omniscient.Web.Clients;

public class IndexerClient(HttpClient httpClient, ILogger<IndexerClient> logger)
{
    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        logger.LogInformation("SearchEmailsAsync: {query}", query);
        var response = await httpClient.GetAsync($"api/indexer?query={query}&pageNumber={pageNumber}&pageSize={pageSize}");
        logger.LogInformation("SearchEmailsAsync Response: {response}", response);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>();
    }
    
    public async Task<string> GetFullContentAsync(Guid id)
    {
        logger.LogInformation("GetFullContentAsync: {id}", id);
        var response = await httpClient.GetAsync($"api/indexer/full-content/{id}");
        logger.LogInformation("GetFullContentAsync Response: {response}", response);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}