using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Modules.Projects.Module.Domain;

namespace ProjectHub.Modules.Projects.Module.Persistence.Configurations;

internal class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable(ProjectsDbContext.TablePrefix + "projects");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.WorkspaceId)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.Description)
            .HasMaxLength(2000);
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Planning");
        
        builder.Property(x => x.StartDate);
        
        builder.Property(x => x.DueDate);
        
        builder.Property(x => x.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.WorkspaceId);
        builder.HasIndex(x => x.Status);
    }
}
