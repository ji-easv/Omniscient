using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;

namespace Omniscient.Web.Clients;

public class IndexerClient(HttpClient httpClient)
{
    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageNumber = 1, int pageSize = 10)
    {
        var response = await httpClient.GetAsync($"api/indexer?query={query}&pageNumber={pageNumber}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedList<EmailDto>>();
    }
    
    public async Task<Email> GetEmailAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"api/indexer/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Email>();
    }
}