using ECommerceDogfood.Web.Models;
using Microsoft.EntityFrameworkCore;
using NetMX.EntityFrameworkCore;

namespace ECommerceDogfood.Web.Data;

/// <summary>
/// The main database context for the NetMX application.
/// Inherits from NetMXDbContext to gain framework features like:
/// - Automatic soft-delete filtering
/// - Multi-tenancy support
/// - Audit logging integration
/// - Concurrency checking
/// </summary>
public class AppDbContext : NetMXDbContext<AppDbContext>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Add your module DbSets here
    // Example: public DbSet<YourEntity> YourEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add your module entity configurations here
        // Example:
        // modelBuilder.Entity<YourEntity>(b =>
        // {
        //     b.ToTable("YourEntities");
        //     b.HasKey(x => x.Id);
        // });
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Review> Reviews => Set<Review>();



}
