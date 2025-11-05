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
            else
            {
                // Fallback for unsupported providers at runtime in this template. Keeps compile simple.
                options.UseSqlite("Data Source=todos.db");
            }
        });

        // EF-backed service
        services.AddScoped<ITodoService, EfTodoService>();

        return services;
    }
}
