using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;

namespace KanbanApp.Data;

public class KanbanDbContext : DbContext
{
    public KanbanDbContext(DbContextOptions<KanbanDbContext> options) : base(options)
    {
    }
    
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<KanbanList> Lists => Set<KanbanList>();
    public DbSet<Card> Cards => Set<Card>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Board configuration
        modelBuilder.Entity<Board>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.HasMany(b => b.Lists)
                  .WithOne(l => l.Board)
                  .HasForeignKey(l => l.BoardId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(b => b.Position);
        });
        
        // KanbanList configuration
        modelBuilder.Entity<KanbanList>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Name).IsRequired().HasMaxLength(200);
            entity.HasMany(l => l.Cards)
                  .WithOne(c => c.List)
                  .HasForeignKey(c => c.ListId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(l => new { l.BoardId, l.Position });
        });
        
        // Card configuration
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(500);
            entity.HasIndex(c => new { c.ListId, c.Position });
        });
        
        // Seed data
        SeedData(modelBuilder);
    }
    
    private void SeedData(ModelBuilder modelBuilder)
    {
        // Static date for seed data
        var baseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Seed Boards
        modelBuilder.Entity<Board>().HasData(
            new Board { Id = 1, Name = "Product Roadmap", Description = "Track product features and releases", Position = 0, IsArchived = false, CreatedAt = baseDate.AddDays(-30) },
            new Board { Id = 2, Name = "Sprint Planning", Description = "Current sprint tasks", Position = 1, IsArchived = false, CreatedAt = baseDate.AddDays(-14) }
        );
        
        // Seed Lists for Board 1 (Product Roadmap)
        modelBuilder.Entity<KanbanList>().HasData(
            new { Id = 1, BoardId = 1, Name = "Backlog", Position = 0, IsArchived = false },
            new { Id = 2, BoardId = 1, Name = "In Progress", Position = 1, IsArchived = false },
            new { Id = 3, BoardId = 1, Name = "Done", Position = 2, IsArchived = false }
        );
        
        // Seed Lists for Board 2 (Sprint Planning)
        modelBuilder.Entity<KanbanList>().HasData(
            new { Id = 4, BoardId = 2, Name = "To Do", Position = 0, IsArchived = false },
            new { Id = 5, BoardId = 2, Name = "Doing", Position = 1, IsArchived = false },
            new { Id = 6, BoardId = 2, Name = "Review", Position = 2, IsArchived = false },
            new { Id = 7, BoardId = 2, Name = "Completed", Position = 3, IsArchived = false }
        );
        
        // Seed Cards for Board 1, List 1 (Backlog)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 1, ListId = 1, Title = "User Authentication", Description = "Implement OAuth2 login", Position = 0, Priority = CardPriority.High, CreatedAt = baseDate.AddDays(-20) },
            new { Id = 2, ListId = 1, Title = "Payment Gateway", Description = "Integrate Stripe payments", Position = 1, Priority = CardPriority.Medium, CreatedAt = baseDate.AddDays(-19) },
            new { Id = 3, ListId = 1, Title = "Email Notifications", Description = "Send transactional emails", Position = 2, Priority = CardPriority.Low, CreatedAt = baseDate.AddDays(-18) }
        );
        
        // Seed Cards for Board 1, List 2 (In Progress)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 4, ListId = 2, Title = "Database Schema", Description = "Design EF Core models", Position = 0, Priority = CardPriority.High, CreatedAt = baseDate.AddDays(-10) },
            new { Id = 5, ListId = 2, Title = "API Endpoints", Description = "Build REST API", Position = 1, Priority = CardPriority.High, CreatedAt = baseDate.AddDays(-9) }
        );
        
        // Seed Cards for Board 1, List 3 (Done)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 6, ListId = 3, Title = "Project Setup", Description = "Initialize project with .NET 9", Position = 0, Priority = CardPriority.High, CreatedAt = baseDate.AddDays(-30) },
            new { Id = 7, ListId = 3, Title = "CI/CD Pipeline", Description = "Setup GitHub Actions", Position = 1, Priority = CardPriority.Medium, CreatedAt = baseDate.AddDays(-25) }
        );
        
        // Seed Cards for Board 2, List 4 (To Do)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 8, ListId = 4, Title = "Sprint Planning Meeting", Description = "Plan next 2 weeks", Position = 0, Priority = CardPriority.Urgent, CreatedAt = baseDate.AddDays(-7), DueDate = baseDate.AddDays(1) },
            new { Id = 9, ListId = 4, Title = "Bug Fix: Login Error", Description = "Fix 401 error on login", Position = 1, Priority = CardPriority.Urgent, CreatedAt = baseDate.AddDays(-5) }
        );
        
        // Seed Cards for Board 2, List 5 (Doing)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 10, ListId = 5, Title = "Implement Search", Description = "Add search functionality", Position = 0, Priority = CardPriority.High, CreatedAt = baseDate.AddDays(-3) }
        );
        
        // Seed Cards for Board 2, List 6 (Review)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 11, ListId = 6, Title = "Code Review: Auth Module", Description = "Review PR #42", Position = 0, Priority = CardPriority.Medium, CreatedAt = baseDate.AddDays(-2) }
        );
        
        // Seed Cards for Board 2, List 7 (Completed)
        modelBuilder.Entity<Card>().HasData(
            new { Id = 12, ListId = 7, Title = "Deploy to Staging", Description = "Deploy latest version", Position = 0, Priority = CardPriority.Medium, CreatedAt = baseDate.AddDays(-1) }
        );
    }
}
