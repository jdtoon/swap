using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectHub.Modules.Tasks.Module.Persistence;

internal static class ServiceCollectionExtensions
{
    private const string ProviderKey = "Data:Provider";

    public static IServiceCollection AddTasksPersistence(this IServiceCollection services, IConfiguration config)
    {
        var provider = (config[ProviderKey] ?? "Sqlite").Trim();
        var conn = config.GetConnectionString("Tasks")
                   ?? config.GetConnectionString("Default")
                   ?? "Data Source=tasks.db";

        services.AddDbContext<TasksDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(conn);
            }
            else
            {
                // Default to SQLite for development
                options.UseSqlite("Data Source=tasks.db");
            }
        });

        services.AddHostedService<TasksDatabaseInitializer>();

        return services;
    }
}
