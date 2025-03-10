using Microsoft.EntityFrameworkCore;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure.Repository;

public class IndexerDbContext(DbContextOptions<IndexerDbContext> options) : DbContext(options)
{
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<Word> Words { get; set; } = null!;
    public DbSet<Occurence> Occurrences { get; set; } = null!;
}