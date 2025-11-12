using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProjectHub.Modules.Tasks.Module.Persistence;

internal class TasksDatabaseInitializer(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async System.Threading.Tasks.Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetService<TasksDbContext>();
        if (db is null) return;

        var provider = (configuration["Data:Provider"] ?? "Sqlite").Trim();
        var migrateConfigured = bool.TryParse(configuration["Data:MigrateOnStartup"], out var m) && m;
        var shouldInit = migrateConfigured || string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase);
        if (!shouldInit) return;

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            var hasMigrations = db.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
    }

    public System.Threading.Tasks.Task StopAsync(CancellationToken cancellationToken) => System.Threading.Tasks.Task.CompletedTask;
}
