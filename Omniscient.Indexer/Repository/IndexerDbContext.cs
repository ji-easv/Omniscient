using Microsoft.EntityFrameworkCore;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer;

public class IndexerDbContext : DbContext
{
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<Word> Words { get; set; } = null!;
    public DbSet<Occurence> Occurrences { get; set; } = null!;

    public IndexerDbContext(DbContextOptions<IndexerDbContext> options) : base(options)
    {
    }
    
    
}