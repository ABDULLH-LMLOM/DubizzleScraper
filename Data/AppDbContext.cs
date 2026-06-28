using DubizzleScraper.Models;
using Microsoft.EntityFrameworkCore;

namespace DubizzleScraper.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // فقط Properties — مفيش SearchFilters في الداتابيز
    public DbSet<Property> Properties { get; set; }
    public DbSet<SearchFilter> SearchFilters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Property>()
            .HasIndex(p => p.DubizzleId)
            .IsUnique();

        modelBuilder.Entity<Property>()
            .HasIndex(p => p.NotificationSent);

        modelBuilder.Entity<Property>()
            .HasIndex(p => p.ScrapedAt);
    }
}