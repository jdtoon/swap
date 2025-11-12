using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectHub.Modules.Workspaces.Module.Domain;

namespace ProjectHub.Modules.Workspaces.Module.Persistence.Configurations;

internal class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable(WorkspacesDbContext.TablePrefix + "workspaces");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.Description)
            .HasMaxLength(1000);
        
        builder.Property(x => x.Color)
            .HasMaxLength(20);
        
        builder.Property(x => x.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt);
    }
}
