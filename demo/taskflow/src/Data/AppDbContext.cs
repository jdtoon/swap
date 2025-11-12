using Microsoft.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure TaskItem
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        });
        
        // Seed task examples
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem 
            { 
                Id = 1, 
                Title = "Set up project infrastructure", 
                Description = "Configure CI/CD, database, and hosting",
                Status = TaskItemStatus.Done,
                Priority = TaskPriority.High,
                AssignedTo = "DevOps Team",
                CreatedAt = new DateTime(2025, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                CompletedAt = new DateTime(2025, 11, 7, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem 
            { 
                Id = 2, 
                Title = "Design task board UI", 
                Description = "Create mockups for the task management interface",
                Status = TaskItemStatus.InProgress,
                Priority = TaskPriority.Medium,
                AssignedTo = "Design Team",
                DueDate = new DateTime(2025, 11, 14, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 11, 9, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem 
            { 
                Id = 3, 
                Title = "Implement authentication", 
                Description = "Add user login and registration features",
                Status = TaskItemStatus.Todo,
                Priority = TaskPriority.Critical,
                AssignedTo = "Backend Team",
                DueDate = new DateTime(2025, 11, 17, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 11, 11, 0, 0, 0, DateTimeKind.Utc)
            },
            new TaskItem 
            { 
                Id = 4, 
                Title = "Write API documentation", 
                Description = "Document all REST endpoints and data models",
                Status = TaskItemStatus.Todo,
                Priority = TaskPriority.Low,
                AssignedTo = "Documentation Team",
                DueDate = new DateTime(2025, 11, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 11, 12, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
