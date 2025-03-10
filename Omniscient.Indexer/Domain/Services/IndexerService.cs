using Omniscient.Indexer.Infrastructure.Repository;
using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Domain.Services;

public class IndexerService(IIndexerRepository indexerRepository) : IIndexerService 
{
    public async Task<Email> GetEmailAsync(Guid emailId)
    {
        var email = await indexerRepository.GetEmailAsync(emailId);
        
        if (email == null)
        {
            throw new Exception($"Email with id {emailId} not found");
        }
        
        return email;
    }
    
    public async Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize)
    {
        return await indexerRepository.SearchEmailsAsync(query, pageIndex, pageSize);
    }
}