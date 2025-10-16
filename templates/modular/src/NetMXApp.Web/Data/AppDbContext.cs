using Microsoft.EntityFrameworkCore;
using NetMX.EntityFrameworkCore;

namespace NetMXApp.Web.Data;

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

    // Add your DbSets here as you create entities
    // Example:
    // public DbSet<Product> Products { get; set; }
    // public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure your entity mappings here
        // Example:
        // modelBuilder.Entity<Product>(b =>
        // {
        //     b.ToTable("Products");
        //     b.HasKey(x => x.Id);
        //     b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        // });
    }
}
