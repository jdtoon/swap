using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Swap.Patterns.Versionable;

/// <summary>
/// EF Core interceptor that automatically initializes and increments Version
/// for entities implementing IVersionable.
/// </summary>
public class VersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateVersions(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateVersions(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateVersions(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries<IVersionable>();
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Initialize to 1 on first insert if default(0)
                    if (entry.Entity.Version <= 0)
                    {
                        entry.Entity.Version = 1;
                    }
                    break;
                case EntityState.Modified:
                    // Increment on update; stay >= 1
                    entry.Entity.Version = Math.Max(1, entry.Entity.Version) + 1;
                    break;
            }
        }
    }
}
