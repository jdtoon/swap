namespace Swap.Patterns.Visibility;

/// <summary>
/// Extension methods for working with entities that implement IVisibility.
/// </summary>
public static class VisibilityExtensions
{
    /// <summary>
    /// Manually show/enable the entity (sets IsVisible = true).
    /// Does not affect VisibleFrom/VisibleUntil.
    /// </summary>
    public static void Show(this IVisibility entity)
    {
        entity.IsVisible = true;
    }

    /// <summary>
    /// Manually hide/disable the entity (sets IsVisible = false).
    /// Does not affect VisibleFrom/VisibleUntil.
    /// </summary>
    public static void Hide(this IVisibility entity)
    {
        entity.IsVisible = false;
    }

    /// <summary>
    /// Schedule the entity to become visible at a future UTC date/time.
    /// Sets IsVisible = true and VisibleFrom = scheduledUtc, clears VisibleUntil.
    /// </summary>
    public static void ScheduleVisibility(this IVisibility entity, DateTime scheduledUtc)
    {
        entity.IsVisible = true;
        entity.VisibleFrom = scheduledUtc;
        entity.VisibleUntil = null;
    }

    /// <summary>
    /// Schedule the entity to be visible within a time window (UTC).
    /// Sets IsVisible = true, VisibleFrom = fromUtc, VisibleUntil = untilUtc.
    /// </summary>
    public static void ScheduleVisibilityWindow(this IVisibility entity, DateTime fromUtc, DateTime untilUtc)
    {
        entity.IsVisible = true;
        entity.VisibleFrom = fromUtc;
        entity.VisibleUntil = untilUtc;
    }

    /// <summary>
    /// Clear all scheduling and make visible immediately (IsVisible = true, no time restrictions).
    /// </summary>
    public static void ShowNow(this IVisibility entity)
    {
        entity.IsVisible = true;
        entity.VisibleFrom = null;
        entity.VisibleUntil = null;
    }

    /// <summary>
    /// Evaluates whether the entity is currently visible based on IsVisible flag and time window.
    /// Checks current UTC time against VisibleFrom/VisibleUntil.
    /// </summary>
    public static bool IsCurrentlyVisible(this IVisibility entity)
    {
        if (!entity.IsVisible) return false;

        var now = DateTime.UtcNow;

        if (entity.VisibleFrom.HasValue && now < entity.VisibleFrom.Value)
            return false;

        if (entity.VisibleUntil.HasValue && now > entity.VisibleUntil.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Filters query to only entities that are currently visible (based on IsVisible and time window).
    /// </summary>
    public static IQueryable<T> Visible<T>(this IQueryable<T> query) where T : class, IVisibility
    {
        var now = DateTime.UtcNow;
        return query.Where(e =>
            e.IsVisible &&
            (e.VisibleFrom == null || e.VisibleFrom <= now) &&
            (e.VisibleUntil == null || e.VisibleUntil > now)
        );
    }

    /// <summary>
    /// Filters query to only entities that are hidden (IsVisible = false OR outside time window).
    /// </summary>
    public static IQueryable<T> Hidden<T>(this IQueryable<T> query) where T : class, IVisibility
    {
        var now = DateTime.UtcNow;
        return query.Where(e =>
            !e.IsVisible ||
            (e.VisibleFrom != null && e.VisibleFrom > now) ||
            (e.VisibleUntil != null && e.VisibleUntil <= now)
        );
    }

    /// <summary>
    /// Filters query to entities scheduled to become visible in the future.
    /// </summary>
    public static IQueryable<T> Scheduled<T>(this IQueryable<T> query) where T : class, IVisibility
    {
        var now = DateTime.UtcNow;
        return query.Where(e =>
            e.IsVisible &&
            e.VisibleFrom != null &&
            e.VisibleFrom > now
        );
    }

    /// <summary>
    /// Filters query to entities that have expired (VisibleUntil in the past).
    /// </summary>
    public static IQueryable<T> Expired<T>(this IQueryable<T> query) where T : class, IVisibility
    {
        var now = DateTime.UtcNow;
        return query.Where(e =>
            e.VisibleUntil != null &&
            e.VisibleUntil <= now
        );
    }
}
