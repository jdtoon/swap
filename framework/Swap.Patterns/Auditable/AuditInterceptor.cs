using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Swap.Patterns.Auditable;

/// <summary>
/// EF Core interceptor that automatically populates audit properties on IAuditable entities.
/// </summary>
/// <remarks>
/// Add this interceptor to your DbContext to enable automatic audit tracking:
/// <code>
/// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
/// {
///     optionsBuilder.AddInterceptors(new AuditInterceptor(() => GetCurrentUserId()));
/// }
/// </code>
/// </remarks>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly Func<string?> _userProvider;

    /// <summary>
    /// Creates a new audit interceptor with the specified user provider function.
    /// </summary>
    /// <param name="userProvider">Function that returns the current user identifier (e.g., user ID, email).</param>
    public AuditInterceptor(Func<string?> userProvider)
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
    }

    /// <summary>
    /// Intercepts SaveChanges to populate audit properties before saving.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Intercepts SaveChangesAsync to populate audit properties before saving.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context == null) return;

        var currentUser = _userProvider();
        var now = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries<IAuditable>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.UpdatedAt = null;
                    entry.Entity.UpdatedBy = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser;
                    // Don't modify CreatedAt/CreatedBy
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}
