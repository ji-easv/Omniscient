using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Omniscient.Shared;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public class IndexerRepository(IndexerDbContext context) : IIndexerRepository
{
    public async Task<List<Occurence>> GetAllOccurrencesAsync(Guid emailId)
    {
        var queryable = context.Occurrences
            .Where(o => o.EmailId == emailId)
            .AsQueryable();
        return await queryable.ToListAsync();
    }

    public async Task<Email> AddEmailAsync(Email email)
    {
        var createdEmail = await context.Emails.AddAsync(email);
        await context.SaveChangesAsync();
        return createdEmail.Entity;
    }

    public async Task AddOccurrenceAsync(Occurence occurrence)
    {
        await context.Occurrences.AddAsync(occurrence);
        await context.SaveChangesAsync();
    }

    public async Task AddOccurrencesAsync(IEnumerable<Occurence> occurrences)
    {
        await context.Occurrences.AddRangeAsync(occurrences);
        await context.SaveChangesAsync();
    }

    public void DeleteEmail(Email email)
    {
        context.Emails.Remove(email);
        context.SaveChanges();
    }

    public Task<Email?> GetEmailAsync(Guid emailId)
    {
        return context.Emails.FirstOrDefaultAsync(e => e.Id == emailId);
    }

    public Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize)
    {
        throw new NotImplementedException();
    }
}