using Microsoft.EntityFrameworkCore;
using SwapDebtors.Models;

namespace SwapDebtors.Data;

public class DebtorsDbContext : DbContext
{
    public DebtorsDbContext(DbContextOptions<DebtorsDbContext> options) : base(options) { }

    public DbSet<Debtor> Debtors => Set<Debtor>();
    public DbSet<Debt> Debts => Set<Debt>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Debtor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.HasMany(e => e.Debts)
                  .WithOne(d => d.Debtor)
                  .HasForeignKey(d => d.DebtorId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Debt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BaseCurrency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.TargetCurrency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Rate).HasPrecision(18, 6);
            entity.HasIndex(e => new { e.BaseCurrency, e.TargetCurrency }).IsUnique();
        });

        // Seed some demo data
        modelBuilder.Entity<Debtor>().HasData(
            new Debtor { Id = 1, Name = "John Smith", Email = "john@example.com", Phone = "555-0101", CreatedAt = DateTime.UtcNow },
            new Debtor { Id = 2, Name = "Maria Garcia", Email = "maria@example.com", Phone = "555-0102", CreatedAt = DateTime.UtcNow },
            new Debtor { Id = 3, Name = "Alex Chen", Email = "alex@example.com", Phone = "555-0103", CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Debt>().HasData(
            new Debt { Id = 1, DebtorId = 1, Amount = 150.00m, Currency = "USD", Description = "Lunch money", CreatedAt = DateTime.UtcNow },
            new Debt { Id = 2, DebtorId = 1, Amount = 50.00m, Currency = "EUR", Description = "Concert ticket", CreatedAt = DateTime.UtcNow },
            new Debt { Id = 3, DebtorId = 2, Amount = 200.00m, Currency = "USD", Description = "Car repair help", CreatedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30) },
            new Debt { Id = 4, DebtorId = 3, Amount = 75.50m, Currency = "GBP", Description = "Birthday gift split", CreatedAt = DateTime.UtcNow }
        );
    }
}
