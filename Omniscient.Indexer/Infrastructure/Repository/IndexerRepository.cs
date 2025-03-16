using Microsoft.EntityFrameworkCore;
using Npgsql;
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
    }

    public async Task AddEmailsAsync(List<Email> emails)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        await context.Emails.AddRangeAsync(emails);
    }
    
    public async Task AddEmailAsync(List<Email> emails)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        await context.Emails.AddRangeAsync(emails);
    }

    public async Task<Email?> GetEmailByFileName(string fileName)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        return await context.Emails.FirstOrDefaultAsync(e => e.FileName == fileName);
    }
    
    public async Task<List<Email>> GetAllEmails()
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        return await context.Emails.ToListAsync();
    }

    public async Task UpsertWordsAsync(List<string> wordValues)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

        if (!wordValues.Any())
            return;

        // Get distinct words
        var distinctWords = wordValues.Distinct().ToList();

        // PostgreSQL has a parameter limit of 65,535 parameters
        // Let's use a safe batch size - since each word is one parameter
        const int batchSize = 10000;

        for (int i = 0; i < distinctWords.Count; i += batchSize)
        {
            var batch = distinctWords.Skip(i).Take(batchSize).ToList();

            // Build SQL statement for this batch
            var valuesList = string.Join(", ", batch.Select((w, index) => $"(@p{index})"));
            var sql = $"INSERT INTO \"Words\" (\"Value\") VALUES {valuesList} ON CONFLICT (\"Value\") DO NOTHING";

            var parameters = batch
                .Select((word, index) => new NpgsqlParameter($"@p{index}", word))
                .ToArray();

            await context.Database.ExecuteSqlRawAsync(sql, parameters);
        }
    }

}