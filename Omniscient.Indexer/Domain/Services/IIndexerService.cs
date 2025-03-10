using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Domain.Services;

public interface IIndexerService
{
    Task<Email> GetEmailAsync(Guid emailId);
    
    Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize);
}