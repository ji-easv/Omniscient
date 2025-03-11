using Microsoft.EntityFrameworkCore;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public virtual DbSet<Email> Emails { get; set; } = null!;
    public virtual DbSet<Word> Words { get; set; } = null!;
    public virtual DbSet<Occurence> Occurrences { get; set; } = null!;
}