using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Projects.Module.Domain;

namespace ProjectHub.Modules.Projects.Module.Persistence;

public class ProjectsDbContext : DbContext
{
    public const string DefaultSchema = "projects";
    public const string TablePrefix = "projects_";

    public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
    }
}
