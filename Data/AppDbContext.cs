using CarpetProgressTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace CarpetProgressTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<ProgressHistory> ProgressHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.OrderNumber).IsUnique();
        });

        modelBuilder.Entity<ProgressHistory>(entity =>
        {
            entity.HasOne(p => p.Order)
                .WithMany(o => o.ProgressHistories)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.OrderNumber);
        });
    }
}
