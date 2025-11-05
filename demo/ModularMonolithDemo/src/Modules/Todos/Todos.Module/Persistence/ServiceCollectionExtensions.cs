using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModularMonolithDemo.Modules.Todos.Module.Persistence;

public static class ServiceCollectionExtensions
{
    private const string ProviderKey = "Data:Provider";
    private const string MigrateKey = "Data:MigrateOnStartup";

    public static IServiceCollection AddTodosPersistence(this IServiceCollection services, IConfiguration config)
    {
        var provider = (config[ProviderKey] ?? "Sqlite").Trim();
        var conn = config.GetConnectionString("Todos")
                   ?? config.GetConnectionString("Default")
                   ?? "Data Source=todos.db";

        services.AddDbContext<TodosDbContext>(options =>
        {
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(conn);
            }
            else if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "Sql Server", StringComparison.OrdinalIgnoreCase))
            {
                // Use SQL Server at runtime; point migrations to the shim project
                options.UseSqlServer(conn, b => b.MigrationsAssembly("Todos.Migrations.SqlServer"));
            }
            else if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(conn, b => b.MigrationsAssembly("Todos.Migrations.Postgres"));
            }
            else
            {
                // Fallback for unsupported providers keeps compile simple.
                options.UseSqlite("Data Source=todos.db");
            }
        });

        // EF-backed service
        services.AddScoped<ITodoService, EfTodoService>();

        // Ensure database is initialized on host startup
        services.AddHostedService<TodosDatabaseInitializer>();

        return services;
    }
}
