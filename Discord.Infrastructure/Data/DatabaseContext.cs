using Discord.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Data;

public class DatabaseContext : DbContext
{
    public DbSet<Subscribe> Subscribe { get; set; }
    
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscribe>()
            .HasIndex(u => u.Id)
            .IsUnique();
    }
}