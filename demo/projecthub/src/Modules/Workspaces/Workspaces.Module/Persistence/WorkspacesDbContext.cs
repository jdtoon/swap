using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Workspaces.Module.Domain;

namespace ProjectHub.Modules.Workspaces.Module.Persistence;

public class WorkspacesDbContext : DbContext
{
    public const string DefaultSchema = "workspaces";
    public const string TablePrefix = "workspaces_";

    public WorkspacesDbContext(DbContextOptions<WorkspacesDbContext> options) : base(options)
    {
    }

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkspacesDbContext).Assembly);
    }
}
