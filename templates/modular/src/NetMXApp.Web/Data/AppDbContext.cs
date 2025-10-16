using Identity.Core.Entities;
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

    // Identity Module
    public DbSet<AppUser> Users { get; set; }
    public DbSet<AppRole> Roles { get; set; }
    public DbSet<AppUserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity Module Configuration
        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(256);
            b.Property(x => x.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<AppRole>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<AppUserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasOne<AppRole>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
