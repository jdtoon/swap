using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ProjectHub.Modules.Projects.Module.Persistence;

internal class ProjectsDatabaseInitializer(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetService<ProjectsDbContext>();
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
