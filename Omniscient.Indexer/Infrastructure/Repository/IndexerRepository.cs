using Microsoft.EntityFrameworkCore;
using Omniscient.ServiceDefaults;
using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public class IndexerRepository(AppDbContext context) : IIndexerRepository
{
    public Task<Email?> GetEmailAsync(Guid emailId)
    {
        return context.Emails.FirstOrDefaultAsync(e => e.Id == emailId);
    }

    public async Task<PaginatedList<Email>> SearchEmailsAsync(string[] queryTerms, int pageIndex, int pageSize)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Find emails with most occurrences of the query terms
        var allEmailIds = await context.Occurrences
            .Where(o => queryTerms.Contains(o.WordValue))
            .GroupBy(o => o.EmailId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Distinct()
            .ToListAsync();
        
        // Get the emails
        var allEmails = await context.Emails
            .Where(e => allEmailIds.Contains(e.Id))
            .ToListAsync();
        
        var emails = allEmails
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new PaginatedList<Email>(emails, allEmails.Count, pageIndex, pageSize);
    }
    
    public async Task AddOccurrencesAsync(IEnumerable<Occurence> occurrences)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        await context.Occurrences.AddRangeAsync(occurrences);
        await context.SaveChangesAsync();
    }

    public async Task<Email> AddEmailAsync(Email email)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        var result =  await context.Emails.AddAsync(email);
        await context.SaveChangesAsync();
        return result.Entity;
    }
    
    public async Task UpsertWordsAsync(List<string> wordValues)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Execute raw SQL using ON CONFLICT DO NOTHING
        foreach (var value in wordValues)
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Words\" (\"Value\") VALUES ({0}) ON CONFLICT (\"Value\") DO NOTHING",
                value);
        }
    }
}