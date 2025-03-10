using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer;

public class IndexerRepository : IIndexerRepository
{
    private readonly IndexerDbContext _context;
    
    public IndexerRepository(IndexerDbContext context)
    {
        _context = context;
    }
    

    public async Task<List<Occurence>> GetAllOccurences(Guid emailId)
    {
        var queryable = _context.Occurrences
            .Where(o => o.EmailId == emailId)
            .AsQueryable();
        return await queryable.ToListAsync();
    }

    public async Task<EntityEntry<Email>> AddEmail(Email email)
    {
        return await _context.Emails.AddAsync(email);
    }

    public async Task AddOccurrence(Occurence occurrence)
    {
        await _context.Occurrences.AddAsync(occurrence);
    }

    public async Task AddOccurrences(IEnumerable<Occurence> occurrences)
    {
        await _context.Occurrences.AddRangeAsync(occurrences);
    }

    public async Task DeleteEmail(Guid emailId)
    {
        _context.Emails.Remove(_context.Emails.Find(emailId));
    }


}