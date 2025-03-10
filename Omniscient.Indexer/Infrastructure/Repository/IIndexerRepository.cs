using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public interface IIndexerRepository
{
    Task<Email?> GetEmailAsync(Guid emailId);
    Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize);
    Task AddWordsAsync(IEnumerable<Word> words);
    Task AddOccurrencesAsync(IEnumerable<Occurence> occurences);
    Task<Email> AddEmailAsync(Email email);
}