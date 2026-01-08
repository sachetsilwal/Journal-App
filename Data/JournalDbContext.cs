using Microsoft.EntityFrameworkCore;
using Journal.Models;

namespace Journal.Data;

public class JournalDbContext : DbContext
{
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    public JournalDbContext(DbContextOptions<JournalDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(e => e.EntryDate)
            .IsUnique(); // Enforce one entry per day at DB level

        base.OnModelCreating(modelBuilder);
    }
}
