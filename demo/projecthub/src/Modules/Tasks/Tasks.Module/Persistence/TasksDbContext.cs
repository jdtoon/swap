using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Tasks.Module.Models;

namespace ProjectHub.Modules.Tasks.Module.Persistence;

internal class TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options)
{
    public DbSet<Models.Task> Tasks => Set<Models.Task>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Task>(entity =>
        {
            entity.ToTable("tasks_tasks");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Description);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.Priority)
                .IsRequired()
                .HasConversion<string>();
            
            entity.Property(e => e.ProjectId)
                .IsRequired();
            
            entity.Property(e => e.AssignedToUserId);
            
            entity.Property(e => e.DueDate);
            
            entity.Property(e => e.Position)
                .IsRequired()
                .HasDefaultValue(0);
            
            entity.Property(e => e.IsArchived)
                .IsRequired()
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            entity.Property(e => e.UpdatedAt);
            
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AssignedToUserId);
            entity.HasIndex(e => new { e.Status, e.Position });
        });
    }
}
