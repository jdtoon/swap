using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Workspaces.Module.Services;

namespace ProjectHub.Modules.Workspaces.Module.Persistence;

public static class ServiceCollectionExtensions
{
    private const string ProviderKey = "Data:Provider";

    public static IServiceCollection AddWorkspacesPersistence(this IServiceCollection services, IConfiguration config)
    {
        var provider = (config[ProviderKey] ?? "Sqlite").Trim();
        var conn = config.GetConnectionString("Workspaces")
                   ?? config.GetConnectionString("Default")
                   ?? "Data Source=projecthub.db";

        services.AddDbContext<WorkspacesDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(conn);
            }
            else if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "Sql Server", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(conn, b => b.MigrationsAssembly("Workspaces.Migrations.SqlServer"));
            }
            else if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(conn, b => b.MigrationsAssembly("Workspaces.Migrations.Postgres"));
            }
            else
            {
                options.UseSqlite("Data Source=projecthub.db");
            }
        });

        services.AddScoped<IWorkspaceService, EfWorkspaceService>();
        services.AddHostedService<WorkspacesDatabaseInitializer>();

        return services;
    }
}
