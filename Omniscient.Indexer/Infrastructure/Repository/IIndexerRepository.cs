using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public interface IIndexerRepository
{
    Task<Email?> GetEmailAsync(Guid emailId);
    Task<Email> AddEmailAsync(Email email);
    Task<PaginatedList<Email>> SearchEmailsAsync(string[] queryTerms, int pageIndex, int pageSize);
    Task AddOccurrencesAsync(IEnumerable<Occurence> occurrences);
    Task UpsertWordsAsync(List<string> words);
}