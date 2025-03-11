using Microsoft.EntityFrameworkCore;
using Omniscient.ServiceDefaults;
using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public class IndexerRepository(AppDbContext context) : IIndexerRepository
{
    public Task<Email?> GetEmailByIdAsync(Guid emailId)
    {
        return context.Emails.FirstOrDefaultAsync(e => e.Id == emailId);
    }

    public Task DeleteEmailAsync(Email email)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        context.Emails.Remove(email);
        return context.SaveChangesAsync();
    }

    public async Task<PaginatedList<Email>> SearchEmailsAsync(string[] queryTerms, int pageIndex, int pageSize)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

        // Find email IDs with most occurrences of the query terms
        var emailScoresQuery = context.Occurrences
            .Where(o => queryTerms.Contains(o.WordValue))
            .GroupBy(o => o.EmailId)
            .Select(g => new
            {
                EmailId = g.Key,
                // Number of unique query terms matched (primary ranking factor)
                UniqueTermsMatched = g.Select(o => o.WordValue).Distinct().Count(),
                // Total occurrences as secondary factor
                TotalOccurrences = g.Sum(o => o.Count)
            })
            .OrderByDescending(g => g.UniqueTermsMatched)
            .ThenByDescending(g => g.TotalOccurrences);

        // Get the total count of matching emails
        var totalCount = await emailScoresQuery.CountAsync();

        // Apply pagination to the email IDs query
        var paginatedEmailIds = await emailScoresQuery
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.EmailId)
            .ToListAsync();

        // Get the emails based on the paginated email IDs
        var emails = await context.Emails
            .Where(e => paginatedEmailIds.Contains(e.Id))
            .ToListAsync();
        
        var orderedEmails = paginatedEmailIds
            .Select(id => emails.FirstOrDefault(e => e.Id == id))
            .Where(e => e != null)
            .ToList();

        return new PaginatedList<Email>(orderedEmails, totalCount, pageIndex, pageSize);
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

    public async Task<Email?> GetEmailByFileName(string fileName)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        return await context.Emails.FirstOrDefaultAsync(e => e.FileName == fileName);
    }

    public async Task UpsertWordsAsync(List<string> wordValues)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Execute raw SQL using ON CONFLICT DO NOTHING to avoid duplicate key errors due to race conditions
        foreach (var value in wordValues)
        {
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Words\" (\"Value\") VALUES ({0}) ON CONFLICT (\"Value\") DO NOTHING",
                value);
        }
    }
}