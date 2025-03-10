using Microsoft.EntityFrameworkCore.ChangeTracking;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer;

public interface IIndexerRepository
{
    Task<List<Occurence>> GetAllOccurences(Guid emailId);
    
    Task<EntityEntry<Email>> AddEmail(Email email);
    
    Task AddOccurrence(Occurence occurrence);
    
    Task AddOccurrences(IEnumerable<Occurence> occurrences);
    
    Task DeleteEmail(Guid emailId);
    

}