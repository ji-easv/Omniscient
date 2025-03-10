using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public interface IIndexerRepository
{
    Task<List<Occurence>> GetAllOccurrencesAsync(Guid emailId);
    
    Task<Email> AddEmailAsync(Email email);
    
    Task AddOccurrenceAsync(Occurence occurrence);
    
    Task AddOccurrencesAsync(IEnumerable<Occurence> occurrences);
    
    void DeleteEmail(Email email);
    
    Task<Email?> GetEmailAsync(Guid emailId);

    Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize);
}