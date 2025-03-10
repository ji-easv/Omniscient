using Microsoft.EntityFrameworkCore;
using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public class IndexerRepository(AppDbContext context) : IIndexerRepository
{
    public Task<Email?> GetEmailAsync(Guid emailId)
    {
        return context.Emails.FirstOrDefaultAsync(e => e.Id == emailId);
    }

    public Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize)
    {
        throw new NotImplementedException();
    }

    public async Task AddWordsAsync(IEnumerable<Word> words)
    {
        await context.Words.AddRangeAsync(words);
        await context.SaveChangesAsync();
    }

    public async Task AddOccurrencesAsync(IEnumerable<Occurence> occurences)
    {
        await context.Occurrences.AddRangeAsync(occurences);
        await context.SaveChangesAsync();
    }

    public async Task<Email> AddEmailAsync(Email email)
    {
        var result =  await context.Emails.AddAsync(email);
        await context.SaveChangesAsync();
        return result.Entity;
    }
}