using Microsoft.EntityFrameworkCore;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Infrastructure;

public class AppDbContext : DbContext
{
    public virtual DbSet<Email> Emails { get; set; } = default!;
    public virtual DbSet<Word> Words { get; set; } = default!;
    public virtual DbSet<Occurence> Occurences { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}